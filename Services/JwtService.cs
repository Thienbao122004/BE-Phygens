using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BE_Phygens.Models;
using BE_Phygens.Dto;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace BE_Phygens.Services
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;
        private readonly PhygensContext _context;
        private readonly string _secretKey;
        private readonly string _accessTokenSecret;
        private readonly string _refreshTokenSecret;
        private readonly int _accessTokenExpirationMinutes;
        private readonly int _refreshTokenExpirationDays;
        private readonly string _issuer;
        private readonly string _audience;

        public JwtService(IConfiguration configuration, PhygensContext context)
        {
            _configuration = configuration;
            _context = context;
            
            _secretKey = _configuration["Jwt:SecretKey"] ?? throw new ArgumentNullException("Jwt:SecretKey");
            _accessTokenSecret = _configuration["Jwt:AccessTokenSecret"] ?? _secretKey; // Fallback to SecretKey if not set
            _refreshTokenSecret = _configuration["Jwt:RefreshTokenSecret"] ?? _secretKey; // Fallback to SecretKey if not set
            
            _accessTokenExpirationMinutes = _configuration.GetValue<int>("Jwt:AccessTokenExpirationMinutes", 15);
            _refreshTokenExpirationDays = _configuration.GetValue<int>("Jwt:RefreshTokenExpirationDays", 7);
            
            _issuer = _configuration["Jwt:Issuer"] ?? "BE_Phygens";
            _audience = _configuration["Jwt:Audience"] ?? "BE_Phygens_Client";
        }

        public async Task<LoginResponseDto> GenerateTokenAsync(User user)
        {
            var (accessToken, refreshToken) = GenerateTokens(user);

            return new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                TokenType = "Bearer",
                ExpiresIn = _accessTokenExpirationMinutes * 60, // Convert to seconds
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

        public string GenerateAccessToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_accessTokenSecret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public (string accessToken, string refreshToken) GenerateTokens(User user)
        {
            var accessToken = GenerateAccessToken(user);
            
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId),
                new Claim(ClaimTypes.Name, user.Username)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_refreshTokenSecret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var refreshToken = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(_refreshTokenExpirationDays),
                signingCredentials: credentials
            );

            return (accessToken, new JwtSecurityTokenHandler().WriteToken(refreshToken));
        }

        public ClaimsPrincipal? ValidateRefreshToken(string refreshToken)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _issuer,
                ValidAudience = _audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_refreshTokenSecret))
            };

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(refreshToken, tokenValidationParameters, out SecurityToken securityToken);

                if (securityToken is not JwtSecurityToken jwtSecurityToken || 
                    !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return null;
                }

                return principal;
            }
            catch
            {
                return null;
            }
        }
    }
} 