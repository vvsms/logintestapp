using ClientApp.State;
using SharedDtos;
using System.Net.Http.Json;

namespace ClientApp.Services;

public class AuthService
{
    private readonly HttpClient _http;       // goes through AuthMessageHandler
    private readonly HttpClient _raw;        // sends cookies, no bearer (for refresh/login/register)
    private readonly TokenState _token;

    public AuthService(IHttpClientFactory factory, TokenState token)
    {
        _http = factory.CreateClient("Api");      // authenticated pipeline
        _raw = factory.CreateClient("ApiRaw");   // raw pipeline (cookies)
        _token = token;
    }

    public async Task<bool> RegisterAsync(RegisterRequest req)
    {
        var res = await _raw.PostAsJsonAsync("api/auth/register", req);
        if (!res.IsSuccessStatusCode) return false;
        var data = await res.Content.ReadFromJsonAsync<AuthResponse>();
        if (data == null) return false;
        _token.Set(data.AccessToken, data.ExpiresAtUtc);
        return true;
    }

    public async Task<bool> LoginAsync(LoginRequest req)
    {
        var res = await _raw.PostAsJsonAsync("api/auth/login", req);
        if (!res.IsSuccessStatusCode) return false;
        var data = await res.Content.ReadFromJsonAsync<AuthResponse>();
        if (data == null) return false;
        _token.Set(data.AccessToken, data.ExpiresAtUtc);
        return true;
    }

    public async Task LogoutAsync()
    {
        await _raw.PostAsync("api/auth/logout", null);
        _token.Clear();
    }

    public HttpClient Http => _http; // expose authenticated client for other services
}