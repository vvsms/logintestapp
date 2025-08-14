using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Server.Configurations;
using Server.Data;
using Server.Interfaces;
using Server.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Server.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;

    public TokenService(IConfiguration config, UserManager<ApplicationUser> userManager, ApplicationDbContext db)
    {
        _config = config;
        _userManager = userManager;
        _db = db;
    }

    public string GenerateAccessToken(ApplicationUser user, IList<string> roles)
    {
        var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id),
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, user.UserName ?? ""),
                new(JwtRegisteredClaimNames.Email, user.Email ?? "")
            };
        foreach (var role in roles) claims.Add(new Claim(ClaimTypes.Role, role));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<(string refreshToken, DateTime expiry)> GenerateRefreshTokenAsync(string userId, string? createdByIp = null)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var expiry = DateTime.UtcNow.AddDays(7);

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = userId,
            Token = token,
            ExpiryDate = expiry,
            CreatedByIp = createdByIp ?? ""
        });
        await _db.SaveChangesAsync();
        return (token, expiry);
    }

    public Task<bool> ValidateRefreshTokenAsync(string userId, string token)
    {
        var ok = _db.RefreshTokens.Any(r => r.UserId == userId && r.Token == token && r.IsActive);
        return Task.FromResult(ok);
    }

    public async Task<string> RotateRefreshTokenAsync(string userId, string token, string? revokedByIp = null)
    {
        var rec = _db.RefreshTokens.FirstOrDefault(r => r.UserId == userId && r.Token == token && r.IsActive);
        if (rec is null) throw new SecurityTokenException("Invalid refresh token");

        rec.RevokedAt = DateTime.UtcNow;
        rec.RevokedByIp = revokedByIp;

        var (newToken, _) = await GenerateRefreshTokenAsync(userId, revokedByIp);
        rec.ReplacedByToken = newToken;

        await _db.SaveChangesAsync();
        return newToken;
    }
}
}