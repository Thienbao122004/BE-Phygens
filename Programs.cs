using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using BE_Phygens.Models;

namespace BE_Phygens
{
    public class Programs
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Load cấu hình
            builder.Configuration.AddEnvironmentVariables();

            // JWT Key - ưu tiên JWT_SECRET_KEY hiện tại, fallback JWT_KEY
            var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? 
                           Environment.GetEnvironmentVariable("JWT_KEY") ?? 
                           builder.Configuration["Jwt:SecretKey"];
            
            if (string.IsNullOrEmpty(secretKey))
            {
                throw new InvalidOperationException("JWT_KEY or JWT_SECRET_KEY environment variable is not configured.");
            }
            
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            // Add DbContext - lấy connection string từ environment variable
            var connectionString = Environment.GetEnvironmentVariable("DefaultConnection") ?? 
                                 Environment.GetEnvironmentVariable("DATABASE_URL") ?? 
                                 Environment.GetEnvironmentVariable("SUPABASE_CONNECTION_STRING") ??
                                 builder.Configuration.GetConnectionString("DefaultConnection");
            
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("DefaultConnection, DATABASE_URL or SUPABASE_CONNECTION_STRING environment variable is not configured.");
            }
            
            // Debug: Log connection string (hide password)
            var debugConnectionString = connectionString.Contains("Password=") 
                ? connectionString.Substring(0, connectionString.IndexOf("Password=")) + "Password=***"
                : connectionString;
            Console.WriteLine($"Using connection string: {debugConnectionString}");
            
            builder.Services.AddDbContext<PhygensContext>(options =>
            {
                try 
                {
                    // Parse connection string and add specific options for Railway/Supabase
                    var npgsqlConnectionString = new Npgsql.NpgsqlConnectionStringBuilder(connectionString)
                    {
                        // Force IPv4 and add timeout settings for Railway compatibility
                        Timeout = 30,
                        CommandTimeout = 60,
                        ApplicationName = "BE-Phygens-Railway",
                        // SSL settings for Supabase - more robust configuration
                        SslMode = Npgsql.SslMode.Require,
                        TrustServerCertificate = true,
                        // Additional security settings for Supabase
                        IncludeErrorDetail = true,
                        // Add these for better Railway compatibility
                        KeepAlive = 30,
                        TcpKeepAlive = true,
                        // Pooling settings
                        MaxPoolSize = 20,
                        MinPoolSize = 5
                    };
                    
                    options.UseNpgsql(npgsqlConnectionString.ConnectionString);
                    Console.WriteLine("DbContext configured successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error configuring DbContext: {ex.Message}");
                    throw;
                }
            });

            // Add Authentication (JWT + Google)
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.LoginPath = "/api/LoginGoogle/login-google";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(50);
            })
            .AddJwtBearer(options =>
            {
                var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER");
                var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE");
                
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = !string.IsNullOrEmpty(issuer),
                    ValidIssuer = issuer,
                    ValidateAudience = !string.IsNullOrEmpty(audience),
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            })
            .AddGoogle(googleOptions =>
            {
                googleOptions.ClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID") ?? 
                                        builder.Configuration["Authentication:Google:ClientId"];
                googleOptions.ClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET") ?? 
                                            builder.Configuration["Authentication:Google:ClientSecret"];
                googleOptions.CallbackPath = "/api/LoginGoogle/google-callback";
            });

            // Authorization (nếu bạn cần Role-based policy)
            builder.Services.AddAuthorization();

            // Add Controllers
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Cấu hình cho Railway - lấy port từ environment variable
            var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
            if (!app.Environment.IsDevelopment())
            {
                app.Urls.Clear();
                app.Urls.Add($"http://0.0.0.0:{port}");
            }

            // Chỉ redirect HTTPS trong development
            if (app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            // Swagger chỉ trong Development hoặc có biến môi trường ENABLE_SWAGGER
            if (app.Environment.IsDevelopment() || Environment.GetEnvironmentVariable("ENABLE_SWAGGER") == "true")
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "BE-Phygens API V1");
                    c.RoutePrefix = ""; // Đặt Swagger UI làm trang chủ
                });
            }

            // Health check endpoint
            app.MapGet("/health", async (PhygensContext context) => 
            {
                try
                {
                    var canConnect = await context.Database.CanConnectAsync();
                    var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                    
                    return Results.Ok(new { 
                        status = canConnect ? "healthy" : "database_error",
                        database_connected = canConnect,
                        pending_migrations = pendingMigrations.ToList(),
                        timestamp = DateTime.UtcNow,
                        environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
                    });
                }
                catch (Exception ex)
                {
                    return Results.Json(new { 
                        status = "error", 
                        error = ex.Message,
                        timestamp = DateTime.UtcNow 
                    }, statusCode: 500);
                }
            });

            // Auto migration trên Railway/Production
            if (!app.Environment.IsDevelopment())
            {
                try 
                {
                    using var scope = app.Services.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<PhygensContext>();
                    
                    Console.WriteLine("Checking for pending migrations...");
                    var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                    
                    if (pendingMigrations.Any())
                    {
                        Console.WriteLine($"Found {pendingMigrations.Count()} pending migrations. Applying...");
                        await context.Database.MigrateAsync();
                        Console.WriteLine("Migrations applied successfully!");
                    }
                    else
                    {
                        Console.WriteLine("No pending migrations found.");
                    }
                    
                    // Test connection
                    var canConnect = await context.Database.CanConnectAsync();
                    Console.WriteLine($"Database connection test: {(canConnect ? "SUCCESS" : "FAILED")}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Migration error: {ex.Message}");
                    // Don't stop the app, just log the error
                }
            }

            app.MapControllers();

            app.Run();
        }
    }
}
