using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ClientApp.State;

public class AuthStateProvider : AuthenticationStateProvider
{
    private readonly Services.AuthService _auth;
    private readonly TokenState _state;
    private bool _initialized;

    public AuthStateProvider(Services.AuthService auth, TokenState state)
    {
        _auth = auth;
        _state = state;

        // When TokenState changes (login/logout), notify Blazor.
        _state.Changed += () => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // On first use, try to fetch profile from API (cookie will be sent automatically).
        if (!_initialized)
        {
            var p = await _auth.GetProfileAsync();
            if (p != null)
            {
                _state.SetUser(p.Email, p.FullName, p.Roles);
            }
            _initialized = true;
        }

        ClaimsIdentity identity;
        if (_state.IsAuthenticated)
        {
            var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, _state.Email!),
                    new Claim("fullName", _state.FullName ?? _state.Email!)
                };
            foreach (var r in _state.Roles)
                claims.Add(new Claim(ClaimTypes.Role, r));

            identity = new ClaimsIdentity(claims, authenticationType: "Cookies");
        }
        else
        {
            identity = new ClaimsIdentity();
        }

        return new AuthenticationState(new ClaimsPrincipal(identity));
    }
}
