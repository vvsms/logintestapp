using Client.Auth;
using System.Net.Http.Headers;

namespace Client.Services
{
    public class AuthHeaderHandler(IAccessTokenStore store) : DelegatingHandler
    {
        private readonly IAccessTokenStore _store = store;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = _store.AccessToken;
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return base.SendAsync(request, cancellationToken);
        }
    }
}