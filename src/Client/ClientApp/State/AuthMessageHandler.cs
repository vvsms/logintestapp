using SharedDtos;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ClientApp.State
{
    public class AuthMessageHandler : DelegatingHandler
    {
        private readonly TokenState _token;
        private readonly IHttpClientFactory _factory;

        public AuthMessageHandler(TokenState token, IHttpClientFactory factory)
        {
            _token = token;
            _factory = factory;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Attach current access token if present
            if (!string.IsNullOrWhiteSpace(_token.AccessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token.AccessToken);
            }

            // If token is about to expire, try refresh (cookie will be auto-sent)
            if (_token.AccessToken != null && _token.IsExpiredSoon(TimeSpan.FromSeconds(30)))
            {
                await TryRefreshAsync(cancellationToken);
                if (!string.IsNullOrWhiteSpace(_token.AccessToken))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token.AccessToken);
            }

            var response = await base.SendAsync(request, cancellationToken);

            // On 401, one retry after refresh
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                response.Dispose();
                var refreshed = await TryRefreshAsync(cancellationToken);
                if (refreshed && !string.IsNullOrWhiteSpace(_token.AccessToken))
                {
                    // retry once
                    var clone = await CloneRequestAsync(request);
                    clone.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token.AccessToken);
                    return await base.SendAsync(clone, cancellationToken);
                }
            }

            return response;
        }

        private async Task<bool> TryRefreshAsync(CancellationToken ct)
        {
            var http = _factory.CreateClient("ApiRaw"); // raw client that sends cookies
            var res = await http.PostAsync("api/auth/refresh", content: null, ct);
            if (!res.IsSuccessStatusCode) { _token.Clear(); return false; }

            var data = await res.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: ct);
            if (data == null) { _token.Clear(); return false; }

            _token.Set(data.AccessToken, data.ExpiresAtUtc);
            return true;
        }

        private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri);

            // Copy content
            if (request.Content != null)
            {
                var ms = new MemoryStream();
                await request.Content.CopyToAsync(ms);
                ms.Position = 0;
                clone.Content = new StreamContent(ms);

                foreach (var h in request.Content.Headers)
                    clone.Content.Headers.TryAddWithoutValidation(h.Key, h.Value);
            }

            // Copy headers
            foreach (var h in request.Headers)
                clone.Headers.TryAddWithoutValidation(h.Key, h.Value);

            return clone;
        }
    }
}