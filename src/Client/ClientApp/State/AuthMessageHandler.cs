using SharedDtos;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ClientApp.State
{
    public class AuthMessageHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return base.SendAsync(request, cancellationToken);
        }
    }
}