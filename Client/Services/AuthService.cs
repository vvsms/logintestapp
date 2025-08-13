using Client.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using Shared.DTOs;
using System.Net.Http;
using System.Net.Http.Json;

namespace Client.Services
{
    public class AuthService : IAuthService
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly TokenProvider _tokenProvider;
        private readonly AuthenticationStateProvider _authStateProvider;

        public AuthService(IHttpClientFactory httpFactory, TokenProvider tokenProvider, AuthenticationStateProvider authStateProvider)
        {
            _httpFactory = httpFactory;
            _tokenProvider = tokenProvider;
            _authStateProvider = authStateProvider;
        }

        public string? GetAccessToken() => _tokenProvider.AccessToken;

        public async Task<bool> LoginAsync(string email, string password)
        {
            var client = _httpFactory.CreateClient("NoAuthClient");
            var res = await client.PostAsJsonAsync("api/auth/login", new LoginRequest(email, password));
            if (!res.IsSuccessStatusCode) return false;

            var auth = await res.Content.ReadFromJsonAsync<AuthResponse>();
            if (auth == null) return false;

            // store in memory
            _tokenProvider.SetToken(auth.AccessToken, auth.ExpiresAt.ToUniversalTime());

            // notify auth state provider to re-evaluate
            if (_authStateProvider is MemoryTokenAuthStateProvider memProv)
                memProv.NotifyAuthenticationStateChanged();

            return true;
        }

        public async Task<bool> TrySilentRefreshAsync()
        {
            try
            {
                var client = _httpFactory.CreateClient("NoAuthClient");
                var res = await client.PostAsync("api/auth/refresh", null);
                if (!res.IsSuccessStatusCode) { _tokenProvider.Clear(); return false; }

                var auth = await res.Content.ReadFromJsonAsync<AuthResponse>();
                if (auth == null) { _tokenProvider.Clear(); return false; }

                _tokenProvider.SetToken(auth.AccessToken, auth.ExpiresAt.ToUniversalTime());

                if (_authStateProvider is MemoryTokenAuthStateProvider memProv)
                    memProv.NotifyAuthenticationStateChanged();

                return true;
            }
            catch
            {
                _tokenProvider.Clear();
                return false;
            }
        }

        public async Task LogoutAsync()
        {
            try
            {
                var client = _httpFactory.CreateClient("NoAuthClient");
                await client.PostAsync("api/auth/logout", null);
            }
            catch { /* ignore */ }

            _tokenProvider.Clear();
            if (_authStateProvider is MemoryTokenAuthStateProvider memProv)
                memProv.NotifyAuthenticationStateChanged();
        }
    }
}