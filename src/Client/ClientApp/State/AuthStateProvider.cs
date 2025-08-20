using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace ClientApp.State;

public class AuthStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _storage;
    private const string AccessKey = "access_token";
    private const string EmailKey = "user_email";
    private const string RolesKey = "user_roles";

    public AuthStateProvider(ILocalStorageService storage) { _storage = storage; }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _storage.GetItemAsStringAsync(AccessKey);
        if (string.IsNullOrWhiteSpace(token))
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        var email = await _storage.GetItemAsStringAsync(EmailKey) ?? "";
        var roles = await _storage.GetItemAsync<string[]>(RolesKey) ?? Array.Empty<string>();

        var claims = new List<Claim> { new(ClaimTypes.Name, email) };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var identity = new ClaimsIdentity(claims, "jwt");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public async Task SetUserAsync(string? token, string? email, IEnumerable<string> roles)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            await _storage.RemoveItemAsync(AccessKey);
            await _storage.RemoveItemAsync(EmailKey);
            await _storage.RemoveItemAsync(RolesKey);
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
            return;
        }

        await _storage.SetItemAsStringAsync(AccessKey, token);
        await _storage.SetItemAsStringAsync(EmailKey, email ?? "");
        await _storage.SetItemAsync(RolesKey, roles.ToArray());
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}