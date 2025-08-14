using Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Client.Auth
{
    public sealed class MemoryTokenAuthStateProvider(IAccessTokenStore store)
        : AuthenticationStateProvider
    {
        private readonly IAccessTokenStore _store = store;

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            if (string.IsNullOrWhiteSpace(_store.AccessToken))
            {
                var anon = new ClaimsPrincipal(new ClaimsIdentity());
                return Task.FromResult(new AuthenticationState(anon));
            }
            // We trust the server-validated token. Optionally parse for roles/claims.
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "AuthenticatedUser")
            }, authenticationType: "jwt");

            var user = new ClaimsPrincipal(identity);
            return Task.FromResult(new AuthenticationState(user));
        }

        // Allow AuthService to force a refresh when tokens change
        public void NotifyAuthStateChanged() => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}