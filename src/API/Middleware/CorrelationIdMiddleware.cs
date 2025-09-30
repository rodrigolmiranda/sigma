using System.Diagnostics;
using Microsoft.AspNetCore.Http.Extensions;

namespace Sigma.API.Middleware;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;
    private const string CorrelationIdHeaderName = "X-Correlation-ID";
    private const string TraceIdHeaderName = "X-Trace-ID";

    public CorrelationIdMiddleware(
        RequestDelegate next,
        ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Try to get correlation ID from request header
        string correlationId = context.Request.Headers[CorrelationIdHeaderName].FirstOrDefault()
            ?? context.Request.Headers["X-Request-ID"].FirstOrDefault()
            ?? GenerateCorrelationId();

        // Set the correlation ID in various places for propagation
        context.TraceIdentifier = correlationId;
        context.Items["CorrelationId"] = correlationId;

        // Add to response headers
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.Append(CorrelationIdHeaderName, correlationId);

            // Also add trace ID from Activity if available
            if (Activity.Current != null)
            {
                context.Response.Headers.Append(TraceIdHeaderName, Activity.Current.TraceId.ToString());
            }

            return Task.CompletedTask;
        });

        // Create a new activity with the correlation ID
        using var activity = Activity.Current?.Source?.StartActivity(
            "HTTP " + context.Request.Method + " " + context.Request.Path,
            ActivityKind.Server,
            parentContext: default,
            tags: new Dictionary<string, object?>
            {
                ["correlation.id"] = correlationId,
                ["http.method"] = context.Request.Method,
                ["http.url"] = context.Request.GetDisplayUrl(),
                ["http.host"] = context.Request.Host.ToString(),
                ["http.scheme"] = context.Request.Scheme,
                ["user.agent"] = context.Request.Headers.UserAgent.ToString()
            });

        // Log the request with correlation ID
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["RequestPath"] = context.Request.Path.ToString(),
            ["RequestMethod"] = context.Request.Method
        }))
        {
            _logger.LogInformation(
                "Processing request: Method={Method}, Path={Path}, CorrelationId={CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                correlationId);

            try
            {
                await _next(context);

                // Log successful response
                _logger.LogInformation(
                    "Request completed: Method={Method}, Path={Path}, StatusCode={StatusCode}, CorrelationId={CorrelationId}",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    correlationId);
            }
            catch (Exception ex)
            {
                // Log error with correlation ID
                _logger.LogError(ex,
                    "Request failed: Method={Method}, Path={Path}, CorrelationId={CorrelationId}",
                    context.Request.Method,
                    context.Request.Path,
                    correlationId);

                throw;
            }
        }
    }

    private static string GenerateCorrelationId()
    {
        return $"{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}".Substring(0, 32);
    }
}

public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CorrelationIdMiddleware>();
    }
}