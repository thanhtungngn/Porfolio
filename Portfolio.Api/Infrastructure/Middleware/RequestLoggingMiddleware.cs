namespace Portfolio.Api.Infrastructure.Middleware;

public sealed class RequestLoggingMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context, ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("Request");
        var start = DateTime.UtcNow;

        await next(context);

        var elapsed = DateTime.UtcNow - start;
        logger.LogInformation(
            "Request completed {Method} {Path} with {StatusCode} in {ElapsedMs}ms",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            elapsed.TotalMilliseconds);
    }
}
