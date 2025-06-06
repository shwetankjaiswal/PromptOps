using AppserverMCP.Middleware;

namespace AppserverMCP.Extensions;

public static class MiddlewareExtensions
{
    /// <summary>
    /// Adds detailed request logging middleware to the application pipeline
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <returns>The application builder for chaining</returns>
    public static IApplicationBuilder UseDetailedRequestLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestLoggingMiddleware>();
    }
} 