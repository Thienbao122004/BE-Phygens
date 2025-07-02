using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using BE_Phygens.Models;
using BE_Phygens.Services;

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
            var connectionString = Environment.GetEnvironmentVariable("ConnectDB") ?? 
                                 Environment.GetEnvironmentVariable("DATABASE_URL") ?? 
                                 Environment.GetEnvironmentVariable("SUPABASE_CONNECTION_STRING") ??
                                 builder.Configuration.GetConnectionString("ConnectDB");
            
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("ConnectDB, DATABASE_URL or SUPABASE_CONNECTION_STRING environment variable is not configured.");
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
                options.LoginPath = "/LoginGoogle/login-google";
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
                
                // Custom event để tự động thêm "Bearer " prefix
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var authorization = context.Request.Headers["Authorization"].FirstOrDefault();
                        
                        if (!string.IsNullOrEmpty(authorization) && !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                        {
                            // Nếu không bắt đầu bằng "Bearer ", tự động thêm vào
                            context.Request.Headers["Authorization"] = $"Bearer {authorization}";
                        }
                        
                        return Task.CompletedTask;
                    }
                };
            });
            // Authorization (nếu bạn cần Role-based policy)
            builder.Services.AddAuthorization();

            // Register custom services
            builder.Services.AddScoped<IJwtService, JwtService>();
            builder.Services.AddScoped<IAutoGradingService, AutoGradingService>();
            
            // Add HttpClient for AI services
            builder.Services.AddHttpClient();
            
            // Add Memory Cache for AI response caching
            builder.Services.AddMemoryCache();
            
            // Register AI Service
            builder.Services.AddScoped<BE_Phygens.Services.IAIService, BE_Phygens.Services.AIService>();

            // Add CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
                
                options.AddPolicy("Development", policy =>
                {
                    policy.WithOrigins("http://localhost:5173", "http://localhost:5174", "http://localhost:3000")
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });
            });

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {

                    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                    options.JsonSerializerOptions.WriteIndented = true;
                    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
                });
            builder.Services.AddEndpointsApiExplorer();
            
            // Configure Swagger with JWT Authentication
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo 
                { 
                    Title = "BE-Phygens API", 
                    Version = "v1",
                    Description = "API cho hệ thống giáo dục Vật lý Phygens"
                });

                // Add JWT Authentication to Swagger
                c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Description = "Chỉ cần nhập giá trị token của bạn thôi nhé!.\n\nExample: \"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\"",
                    Name = "Authorization",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT"
                });

                c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                });

                // Include XML comments if available
                var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }
            });

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

            // Add request logging middleware
            app.Use(async (context, next) =>
            {
                var logger = context.RequestServices.GetRequiredService<ILogger<Programs>>();
                logger.LogInformation($"Request: {context.Request.Method} {context.Request.Path} from {context.Connection.RemoteIpAddress}");
                
                await next();
                
                logger.LogInformation($"Response: {context.Response.StatusCode} for {context.Request.Method} {context.Request.Path}");
            });

            app.UseRouting();

            // Enable static file serving for uploads
            app.UseStaticFiles();
            
            // 🎯 Đảm bảo tất cả thư mục cần thiết tồn tại
            var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadsPath = Path.Combine(wwwrootPath, "uploads");
            var nestedUploadsPath = Path.Combine(uploadsPath, "uploads");
            var essayImagesPath = Path.Combine(uploadsPath, "essay-images");
            var chatImagesPath = Path.Combine(wwwrootPath, "chat-images");
            var imagesPath = Path.Combine(wwwrootPath, "images");

            var requiredDirectories = new[] { 
                wwwrootPath, 
                uploadsPath, 
                nestedUploadsPath, 
                essayImagesPath, 
                chatImagesPath, 
                imagesPath 
            };

            try
            {
                foreach (var dir in requiredDirectories)
                {
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                        Console.WriteLine($"✅ Created directory: {dir}");
                    }
                    else
                    {
                        Console.WriteLine($"📁 Directory exists: {dir}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error creating directories: {ex.Message}");
                throw; // Re-throw to prevent app from starting with missing directories
            }
            
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
                RequestPath = "/uploads"
            });

            // Use CORS
            if (app.Environment.IsDevelopment())
            {
                app.UseCors("Development");
            }
            else
            {
                app.UseCors("AllowAll");
            }

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