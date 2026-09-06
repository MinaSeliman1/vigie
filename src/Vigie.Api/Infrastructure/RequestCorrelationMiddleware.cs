using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Vigie.Api.Infrastructure;

public sealed partial class RequestCorrelationMiddleware(RequestDelegate next, ILogger<RequestCorrelationMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = context.Request.Headers["X-Request-Id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(requestId) || requestId.Length > 80 || !RequestIdPattern().IsMatch(requestId))
            requestId = Guid.NewGuid().ToString("N");

        context.TraceIdentifier = requestId;
        context.Response.Headers["X-Request-Id"] = requestId;
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();
            LogRequest(logger, context.Request.Method, context.Request.Path, context.Response.StatusCode, stopwatch.ElapsedMilliseconds, requestId);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs} ms (requestId={RequestId})")]
    private static partial void LogRequest(ILogger logger, string method, PathString path, int statusCode, long elapsedMs, string requestId);

    [GeneratedRegex("^[A-Za-z0-9._-]+$")]
    private static partial Regex RequestIdPattern();
}
