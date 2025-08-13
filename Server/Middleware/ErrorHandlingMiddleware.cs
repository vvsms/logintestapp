using System.Net;
using System.Text.Json;

namespace Server.Middleware
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;
        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next; _logger = logger;
        }
        public async Task Invoke(HttpContext ctx)
        {
            try { await _next(ctx); }
            catch (Exception ex)
            {
                var id = Guid.NewGuid().ToString();
                _logger.LogError(ex, "Unhandled exception {Id}", id);
                ctx.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                ctx.Response.ContentType = "application/json";
                var payload = JsonSerializer.Serialize(new { success = false, message = "Internal error. CorrelationId: " + id, correlationId = id });
                await ctx.Response.WriteAsync(payload);
            }
        }
    }
}