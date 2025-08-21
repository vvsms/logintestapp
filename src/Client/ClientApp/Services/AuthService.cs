using ClientApp.State;
using SharedDtos;
using System.Net.Http.Json;

namespace ClientApp.Services;

public class AuthService(IHttpClientFactory factory, TokenState state)
{
    private readonly HttpClient _raw = factory.CreateClient("ApiRaw");   // sends cookies
    private readonly TokenState _state = state;

    /// <summary>
    /// Register user and automatically login (cookie issued by server).
    /// </summary>
    public async Task<bool> RegisterAsync(RegisterRequest req)
    {
        var res = await _raw.PostAsJsonAsync("api/auth/register", req);
        if (!res.IsSuccessStatusCode) return false;

        var profile = await GetProfileAsync();
        if (profile != null)
        {
            _state.SetUser(profile.Email, profile.FullName, profile.Roles);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Login with credentials. Cookie is set automatically.
    /// </summary>
    public async Task<bool> LoginAsync(LoginRequest req)
    {
        var res = await _raw.PostAsJsonAsync("api/auth/login", req);
        if (!res.IsSuccessStatusCode) return false;

        var profile = await GetProfileAsync();
        if (profile != null)
        {
            _state.SetUser(profile.Email, profile.FullName, profile.Roles);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Logout and clear local state.
    /// </summary>
    public async Task LogoutAsync()
    {
        await _raw.PostAsync("api/auth/logout", null);
        _state.Clear();
    }

    /// <summary>
    /// Gets current profile from API (uses cookie).
    /// </summary>
    public async Task<UserProfile?> GetProfileAsync()
    {
        var resp = await _raw.GetAsync("api/auth/me");
        if (!resp.IsSuccessStatusCode) return null;

        return await resp.Content.ReadFromJsonAsync<UserProfile>();
    }
}

/// <summary>
/// Simple DTO for profile info.
/// </summary>
public class UserProfile
{
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public string[] Roles { get; set; } = [];
}
