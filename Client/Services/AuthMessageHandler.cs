using Client.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net;
using System.Net.Http.Headers;

namespace Client.Services
{
    public class AuthMessageHandler : DelegatingHandler
    {
        private readonly TokenProvider _tokenProvider;
        private readonly IServiceProvider _sp;
        private readonly SemaphoreSlim _refreshLock = new(1, 1);

        public AuthMessageHandler(TokenProvider tokenProvider, IServiceProvider sp)
        {
            _tokenProvider = tokenProvider;
            _sp = sp;
            InnerHandler = new HttpClientHandler();
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = _tokenProvider.AccessToken;
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                // attempt single refresh (thread-safe)
                await _refreshLock.WaitAsync();
                try
                {
                    var auth = _sp.GetService(typeof(IAuthService)) as IAuthService;
                    if (auth != null)
                    {
                        var refreshed = await auth.TrySilentRefreshAsync();
                        if (refreshed && !string.IsNullOrEmpty(_tokenProvider.AccessToken))
                        {
                            // retry original request with new token
                            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenProvider.AccessToken);
                            response.Dispose();
                            return await base.SendAsync(request, cancellationToken);
                        }
                    }
                }
                finally
                {
                    _refreshLock.Release();
                }

                // refresh failed -> clear token
                _tokenProvider.Clear();
                if (_sp.GetService(typeof(AuthenticationStateProvider)) is MemoryTokenAuthStateProvider mem)
                    mem.NotifyAuthenticationStateChanged();
            }

            return response;
        }
    }
}