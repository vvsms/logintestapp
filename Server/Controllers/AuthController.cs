using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Server.Configurations;
using Server.Interfaces;
using Server.Models;
using Server.Services;
using Shared.DTOs;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _config;

    public AuthController(SignInManager<ApplicationUser> signInManager,
                          UserManager<ApplicationUser> userManager,
                          ITokenService tokenService,
                          IConfiguration config)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _tokenService = tokenService;
        _config = config;
    }

    [HttpPost("login")]
    public async Task<ActionResult<TokenResponse>> Login([FromBody] LoginRequest req)
    {
        var user = await _userManager.FindByNameAsync(req.UserName);
        if (user is null) return Unauthorized();

        var passOk = await _signInManager.CheckPasswordAsync(user, req.Password);
        if (!passOk) return Unauthorized();

        var roles = await _userManager.GetRolesAsync(user);
        var access = _tokenService.GenerateAccessToken(user, roles);

        // create refresh token and set cookie (HttpOnly, Secure, SameSite)
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var (refresh, expiry) = await _tokenService.GenerateRefreshTokenAsync(user.Id, ip);

        SetRefreshCookie(refresh, expiry);
        return new TokenResponse { AccessToken = access, ExpiresInMinutes = 15 };
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<TokenResponse>> Refresh()
    {
        if (!Request.Cookies.TryGetValue("rt", out var refresh)) return Unauthorized();

        // The "sub" (user id) is stored in the old access token; since client may have none, we find by token
        var rec = GetRefreshRecord(refresh);
        if (rec is null || !rec.IsActive) return Unauthorized();

        var user = await _userManager.FindByIdAsync(rec.UserId);
        if (user is null) return Unauthorized();

        var valid = await _tokenService.ValidateRefreshTokenAsync(user.Id, refresh);
        if (!valid) return Unauthorized();

        // rotate
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var newRefresh = await _tokenService.RotateRefreshTokenAsync(user.Id, refresh, ip);
        var roles = await _userManager.GetRolesAsync(user);
        var newAccess = _tokenService.GenerateAccessToken(user, roles);

        SetRefreshCookie(newRefresh, DateTime.UtcNow.AddDays(7));
        return new TokenResponse { AccessToken = newAccess, ExpiresInMinutes = 15 };
    }

    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        if (Request.Cookies.ContainsKey("rt"))
        {
            Response.Cookies.Append("rt", "", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UnixEpoch
            });
        }
        return Ok();
    }

    private RefreshToken? GetRefreshRecord(string token)
    {
        // If you use EF, inject ApplicationDbContext and query it here.
        // Example (pseudo):
        // return _db.RefreshTokens.FirstOrDefault(r => r.Token == token);
        return null;
    }

    private void SetRefreshCookie(string token, DateTime expiry)
    {
        Response.Cookies.Append("rt", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = new DateTimeOffset(expiry)
        });
    }
}
}