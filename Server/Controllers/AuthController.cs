using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Server.Configurations;
using Server.Models;
using Server.Services;
using Shared.DTOs;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly TokenService _tokenService;
    private readonly JwtSettings _jwt;

    public AuthController(UserManager<ApplicationUser> userManager, TokenService tokenService, IOptions<JwtSettings> jwtOpt)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _jwt = jwtOpt.Value;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var user = await _userManager.FindByEmailAsync(req.Email);
        if (user == null) return Unauthorized(new { message = "Invalid credentials." });
        var ok = await _userManager.CheckPasswordAsync(user, req.Password);
        if (!ok) return Unauthorized(new { message = "Invalid credentials." });

        var createdByIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
        var (accessToken, refreshToken, expiresAt) = await _tokenService.CreateTokensAsync(user, createdByIp);

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(_jwt.RefreshTokenDays)
        };
        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new AuthResponse(accessToken, expiresAt, roles.ToArray(), Array.Empty<string>()));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        if (!Request.Cookies.TryGetValue("refreshToken", out var refreshToken)) return Unauthorized();

        var (user, tokenEntity) = await _tokenService.ValidateRefreshTokenAsync(refreshToken);
        if (user == null || tokenEntity == null) return Unauthorized();

        var createdByIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
        var newRefreshToken = await _tokenService.RotateRefreshTokenAsync(tokenEntity, createdByIp);

        var (accessToken, _, expiresAt) = await _tokenService.CreateTokensAsync(user, createdByIp);

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(_jwt.RefreshTokenDays)
        };
        Response.Cookies.Append("refreshToken", newRefreshToken, cookieOptions);

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new AuthResponse(accessToken, expiresAt, roles.ToArray(), Array.Empty<string>()));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirst("uid")?.Value;
        if (!string.IsNullOrEmpty(userId)) await _tokenService.RevokeAllRefreshTokensForUserAsync(userId);
        Response.Cookies.Delete("refreshToken");
        return Ok();
    }

    [HttpGet("me/policies")]
    [Authorize]
    public async Task<IActionResult> GetPolicies([FromServices] IAuthorizationService authz)
    {
        var policies = new[] { "CanManageMenus", "CanViewReports" };
        var satisfied = new List<string>();
        foreach (var p in policies)
        {
            var res = await authz.AuthorizeAsync(User, p);
            if (res.Succeeded) satisfied.Add(p);
        }
        return Ok(satisfied);
    }
}