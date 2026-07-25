using System.Diagnostics;

namespace TmsApi.Api.Middlewares;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = Guid.NewGuid().ToString("N")[..8];

        var stopwatch = Stopwatch.StartNew();

        //guarantees header is written for ALL responses
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Correlation-Id"] = correlationId;
            return Task.CompletedTask;
        });

        _logger.LogInformation(
            "[{CorrelationId}] START {Method} {Path}",
            correlationId,
            context.Request.Method,
            context.Request.Path);

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            _logger.LogInformation(
                "[{CorrelationId}] END {StatusCode} ({ElapsedMs} ms)",
                correlationId,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
    }
}