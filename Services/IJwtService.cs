using BE_Phygens.Models;
using BE_Phygens.Dto;

namespace BE_Phygens.Services
{
    public interface IJwtService
    {
        Task<LoginResponseDto> GenerateTokenAsync(User user);
        string? ValidateToken(string token);
        Task<bool> IsTokenValidAsync(string token);
        Task RevokeTokenAsync(string userId, string token);
    }
} 