using Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Client.Auth
{
    public class MemoryTokenAuthStateProvider : AuthenticationStateProvider
    {
        private readonly IAuthService _authService;
        private readonly TokenProvider _tokenProvider;

        public MemoryTokenAuthStateProvider(IAuthService authService, TokenProvider tokenProvider)
        {
            _authService = authService;
            _tokenProvider = tokenProvider;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            // If token valid in memory, build principal
            if (_tokenProvider.HasValidToken())
            {
                var principal = BuildPrincipalFromToken(_tokenProvider.AccessToken!);
                return new AuthenticationState(principal);
            }

            // Try silent refresh (this will set token provider and notify)
            var refreshed = await _authService.TrySilentRefreshAsync();
            if (refreshed && _tokenProvider.HasValidToken())
            {
                var principal = BuildPrincipalFromToken(_tokenProvider.AccessToken!);
                return new AuthenticationState(principal);
            }

            // anonymous
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        public void NotifyAuthenticationStateChanged()
        {
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        private ClaimsPrincipal BuildPrincipalFromToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            var identity = new ClaimsIdentity(jwt.Claims, "jwt");
            return new ClaimsPrincipal(identity);
        }
    }
}