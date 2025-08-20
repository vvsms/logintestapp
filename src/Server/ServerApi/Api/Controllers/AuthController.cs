using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServerApi.Infrastructure.Data;
using ServerApi.Infrastructure.Identity;
using ServerApi.Infrastructure.Security;
using SharedDtos;

namespace ServerApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly RoleManager<IdentityRole> _roles;
    private readonly IJwtTokenService _jwt;
    private readonly ApplicationDbContext _db;
    private readonly IMapper _mapper;
    private const string RefreshCookieName = "refreshToken";

    public AuthController(
        UserManager<ApplicationUser> users,
        SignInManager<ApplicationUser> signIn,
        RoleManager<IdentityRole> roles,
        IJwtTokenService jwt,
        ApplicationDbContext db,
        IMapper mapper)
    {
        _users = users; _signIn = signIn; _roles = roles; _jwt = jwt; _db = db; _mapper = mapper;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest req)
    {
        var exists = await _users.FindByEmailAsync(req.Email);
        if (exists != null) return BadRequest("Email already registered.");

        var user = _mapper.Map<ApplicationUser>(req);
        var result = await _users.CreateAsync(user, req.Password);
        if (!result.Succeeded) return BadRequest(result.Errors);

        if (!await _roles.RoleExistsAsync("User"))
            await _roles.CreateAsync(new IdentityRole("User"));
        await _users.AddToRoleAsync(user, "User");

        var (access, exp) = await _jwt.CreateAccessTokenAsync(user);
        var (refresh, refreshExp) = _jwt.CreateRefreshToken();

        _db.RefreshTokens.Add(new UserRefreshToken { Token = refresh, UserId = user.Id, ExpiresAtUtc = refreshExp });
        await _db.SaveChangesAsync();

        SetRefreshCookie(refresh, refreshExp);

        var roles = await _users.GetRolesAsync(user);
        return new AuthResponse { AccessToken = access, ExpiresAtUtc = exp, Email = user.Email, FullName = user.FullName, Roles = roles };
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest req)
    {
        var user = await _users.FindByEmailAsync(req.Email);
        if (user == null) return Unauthorized("Invalid credentials");
        var passOk = await _signIn.CheckPasswordSignInAsync(user, req.Password, false);
        if (!passOk.Succeeded) return Unauthorized("Invalid credentials");

        var (access, exp) = await _jwt.CreateAccessTokenAsync(user);
        var (refresh, refreshExp) = _jwt.CreateRefreshToken();

        _db.RefreshTokens.Add(new UserRefreshToken { Token = refresh, UserId = user.Id, ExpiresAtUtc = refreshExp });
        await _db.SaveChangesAsync();

        SetRefreshCookie(refresh, refreshExp);

        var roles = await _users.GetRolesAsync(user);
        return new AuthResponse { AccessToken = access, ExpiresAtUtc = exp, Email = user.Email, FullName = user.FullName, Roles = roles };
    }

    // No body: refresh token comes from HttpOnly cookie
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh()
    {
        if (!Request.Cookies.TryGetValue(RefreshCookieName, out var cookieToken) || string.IsNullOrWhiteSpace(cookieToken))
            return Unauthorized("No refresh token");

        var rt = await _db.RefreshTokens.Include(x => x.User).SingleOrDefaultAsync(x => x.Token == cookieToken);
        if (rt == null || rt.Revoked || rt.ExpiresAtUtc < DateTime.UtcNow) return Unauthorized("Invalid refresh token");

        // rotate
        rt.Revoked = true;
        var (newRefresh, newExp) = _jwt.CreateRefreshToken();
        _db.RefreshTokens.Add(new UserRefreshToken { Token = newRefresh, UserId = rt.UserId, ExpiresAtUtc = newExp });

        var (access, accessExp) = await _jwt.CreateAccessTokenAsync(rt.User);
        await _db.SaveChangesAsync();

        SetRefreshCookie(newRefresh, newExp);

        var roles = await _users.GetRolesAsync(rt.User);
        return new AuthResponse { AccessToken = access, ExpiresAtUtc = accessExp, Email = rt.User.Email, FullName = rt.User.FullName, Roles = roles };
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<object>> Me()
    {
        var user = await _users.GetUserAsync(User);
        if (user == null) return Unauthorized();
        var roles = await _users.GetRolesAsync(user);
        return new { user.Email, user.FullName, Roles = roles };
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        // Clear cookie (best-effort; also consider revoking all refresh tokens for user if authenticated)
        Response.Cookies.Delete(RefreshCookieName, new CookieOptions { Path = "/", SameSite = SameSiteMode.None, Secure = true, HttpOnly = true });
        return Ok();
    }

    private void SetRefreshCookie(string token, DateTime expiresUtc)
    {
        // If API and WASM are on different origins in dev, we must use SameSite=None; Secure=true
        Response.Cookies.Append(RefreshCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,                  // requires HTTPS
            SameSite = SameSiteMode.None,   // allows cross-site cookie for SPA on different origin
            Expires = new DateTimeOffset(expiresUtc),
            Path = "/"
        });
    }
}