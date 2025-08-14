using Server.Models;

namespace Server.Interfaces
{
    public interface ITokenService
    {
        string GenerateAccessToken(ApplicationUser user, IList<string> roles);
        Task<(string refreshToken, DateTime expiry)> GenerateRefreshTokenAsync(string userId, string? createdByIp = null);
        Task<bool> ValidateRefreshTokenAsync(string userId, string token);
        Task<string> RotateRefreshTokenAsync(string userId, string token, string? revokedByIp = null);
    }
}
