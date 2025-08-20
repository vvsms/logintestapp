using Blazored.LocalStorage;
using ClientApp.State;
using SharedDtos;
using System.Net.Http.Json;

namespace ClientApp.Services;

public class AuthService
{
    private readonly HttpClient _http;
    private readonly AuthStateProvider _auth;
    private readonly ILocalStorageService _storage;

    private const string AccessKey = "access_token";
    private const string RefreshKey = "refresh_token";

    public AuthService(HttpClient http, AuthStateProvider auth, ILocalStorageService storage)
    { _http = http; _auth = auth; _storage = storage; }

    public async Task<bool> RegisterAsync(RegisterRequest req)
    {
        var res = await _http.PostAsJsonAsync("api/auth/register", req);
        if (!res.IsSuccessStatusCode) return false;

        var data = await res.Content.ReadFromJsonAsync<AuthResponse>();
        await ApplyAuthAsync(data!);
        return true;
    }

    public async Task<bool> LoginAsync(LoginRequest req)
    {
        var res = await _http.PostAsJsonAsync("api/auth/login", req);
        if (!res.IsSuccessStatusCode) return false;
        var data = await res.Content.ReadFromJsonAsync<AuthResponse>();
        await ApplyAuthAsync(data!);
        return true;
    }

    public async Task LogoutAsync() => await _auth.SetUserAsync(null, null, Array.Empty<string>());

    public async Task<AuthResponse?> RefreshAsync()
    {
        var refresh = await _storage.GetItemAsStringAsync(RefreshKey);
        if (string.IsNullOrWhiteSpace(refresh)) return null;
        var res = await _http.PostAsJsonAsync("api/auth/refresh", new RefreshRequest { RefreshToken = refresh });
        if (!res.IsSuccessStatusCode) return null;
        var data = await res.Content.ReadFromJsonAsync<AuthResponse>();
        await ApplyAuthAsync(data!);
        return data;
    }

    private async Task ApplyAuthAsync(AuthResponse data)
    {
        await _storage.SetItemAsStringAsync(AccessKey, data.AccessToken);
        await _storage.SetItemAsStringAsync(RefreshKey, data.RefreshToken);
        await _auth.SetUserAsync(data.AccessToken, data.Email, data.Roles);
        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", data.AccessToken);
    }
}