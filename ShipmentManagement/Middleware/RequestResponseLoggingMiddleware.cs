using System.Net;
using System.Text.Json;

namespace ShipmentManagement.Middleware
{
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

        public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            // Logged request path and query
            _logger.LogInformation("HTTP {method} {path}{query}", context.Request.Method, context.Request.Path, context.Request.QueryString);

            await _next(context);

            // Logged response with status code
            _logger.LogInformation("Response {statusCode}", context.Response.StatusCode);
        }
    }
}
