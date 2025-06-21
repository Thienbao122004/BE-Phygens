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
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Load cấu hình
            builder.Configuration.AddEnvironmentVariables();

            // JWT Key
            var secretKey = builder.Configuration["Jwt:SecretKey"];
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey ?? throw new InvalidOperationException("Jwt:SecretKey is not configured.")));

            // Add DbContext
            
            builder.Services.AddDbContext<PhygensContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("ConnectDB")));

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
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            })
            .AddGoogle(googleOptions =>
            {
                googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"];
                googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
                googleOptions.CallbackPath = "/api/LoginGoogle/google-callback";
            });

            // Authorization (nếu bạn cần Role-based policy)
            builder.Services.AddAuthorization();

            // Add Controllers
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseSwagger();
            app.UseSwaggerUI();

            app.MapControllers();

            app.Run();
        }
    }
}
