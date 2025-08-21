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

        // default: Blazor cookie login
        await _signIn.SignInAsync(user, isPersistent: true);

        return Ok(new { message = "User registered and logged in" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req, [FromQuery] bool useJwt = false)
    {
        var user = await _users.FindByEmailAsync(req.Email);
        if (user == null) return Unauthorized("Invalid credentials");

        var passOk = await _signIn.CheckPasswordSignInAsync(user, req.Password, false);
        if (!passOk.Succeeded) return Unauthorized("Invalid credentials");

        // Mode 1: Blazor WASM (cookie login)
        if (!useJwt)
        {
            await _signIn.SignInAsync(user, isPersistent: true);
            return Ok(new { message = "Login successful (cookie mode)" });
        }

        // Mode 2: Mobile app (JWT + refresh token)
        var (access, exp) = await _jwt.CreateAccessTokenAsync(user);
        var (refresh, refreshExp) = _jwt.CreateRefreshToken();

        _db.RefreshTokens.Add(new UserRefreshToken { Token = refresh, UserId = user.Id, ExpiresAtUtc = refreshExp });
        await _db.SaveChangesAsync();

        SetRefreshCookie(refresh, refreshExp);

        var roles = await _users.GetRolesAsync(user);
        return Ok(new AuthResponse { AccessToken = access, ExpiresAtUtc = exp, Email = user.Email, FullName = user.FullName, Roles = roles });
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
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        Response.Cookies.Delete(RefreshCookieName, new CookieOptions { Path = "/", SameSite = SameSiteMode.None, Secure = true, HttpOnly = true });
        return Ok(new { message = "Logged out" });
    }

    // helper
    private void SetRefreshCookie(string token, DateTime expiresUtc)
    {
        Response.Cookies.Append(RefreshCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = new DateTimeOffset(expiresUtc),
            Path = "/"
        });
    }
}