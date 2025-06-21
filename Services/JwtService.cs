using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BE_Phygens.Models;
using BE_Phygens.Dto;
using Microsoft.EntityFrameworkCore;

namespace BE_Phygens.Services
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;
        private readonly PhygensContext _context;
        private readonly string _secretKey;
        private readonly string? _issuer;
        private readonly string? _audience;
        private readonly int _tokenExpirationHours;

        public JwtService(IConfiguration configuration, PhygensContext context)
        {
            _configuration = configuration;
            _context = context;
            
            // Cùng logic như Programs.cs
            _secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? 
                        Environment.GetEnvironmentVariable("JWT_KEY") ?? 
                        _configuration["Jwt:SecretKey"] ??
                        throw new InvalidOperationException("JWT secret key is not configured");
            
            _issuer = Environment.GetEnvironmentVariable("JWT_ISSUER");
            _audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE");
            _tokenExpirationHours = 24 * 7; // 7 days
        }

        public async Task<LoginResponseDto> GenerateTokenAsync(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secretKey);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserId),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role),
                new("full_name", user.FullName),
                new("is_active", user.IsActive.ToString()),
                new("jti", Guid.NewGuid().ToString()), // Token ID for revocation
                new("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(_tokenExpirationHours),
                Issuer = _issuer,
                Audience = _audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key), 
                    SecurityAlgorithms.HmacSha256Signature
                )
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return new LoginResponseDto
            {
                AccessToken = tokenString,
                TokenType = "Bearer",
                ExpiresIn = _tokenExpirationHours * 3600, // Convert to seconds
                User = new UserResponseDto
                {
                    Id = user.UserId,
                    Username = user.Username,
                    Email = user.Email,
                    FullName = user.FullName,
                    Role = user.Role,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt
                }
            };
        }

        public string? ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_secretKey);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = !string.IsNullOrEmpty(_issuer),
                    ValidIssuer = _issuer,
                    ValidateAudience = !string.IsNullOrEmpty(_audience),
                    ValidAudience = _audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
                var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                return userId;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> IsTokenValidAsync(string token)
        {
            var userId = ValidateToken(token);
            if (string.IsNullOrEmpty(userId))
                return false;

            // Check if user still exists and is active
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId && u.IsActive);

            return user != null;
        }

        public async Task RevokeTokenAsync(string userId, string token)
        {
            // Implement token revocation logic if needed
            // For now, we rely on token expiration
            await Task.CompletedTask;
        }
    }
} 