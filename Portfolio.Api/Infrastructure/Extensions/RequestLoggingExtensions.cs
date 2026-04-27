using Portfolio.Api.Infrastructure.Middleware;

namespace Portfolio.Api.Infrastructure.Extensions;

public static class RequestLoggingExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestLoggingMiddleware>();
    }
}
