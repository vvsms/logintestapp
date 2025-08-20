using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ClientApp.State;

public class AuthStateProvider : AuthenticationStateProvider
{
    private readonly TokenState _token;

    public AuthStateProvider(TokenState token) => _token = token;

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (string.IsNullOrWhiteSpace(_token.AccessToken))
            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

        var handler = new JwtSecurityTokenHandler();
        ClaimsIdentity identity;

        try
        {
            var jwt = handler.ReadJwtToken(_token.AccessToken);
            identity = new ClaimsIdentity(jwt.Claims, "jwt");
        }
        catch
        {
            identity = new ClaimsIdentity();
        }

        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
    }

    public void NotifyChanged() => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
}