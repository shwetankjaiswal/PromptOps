using System.Diagnostics;
using System.Text;

namespace AppserverMCP.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString("N")[..8];
        
        // Log request start
        await LogRequestAsync(context, requestId);

        // Capture response
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            
            // Log response
            //await LogResponseAsync(context, requestId, stopwatch.ElapsedMilliseconds, responseBody);
            
            // Copy response back to original stream
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
        }
    }

    private async Task LogRequestAsync(HttpContext context, string requestId)
    {
        try
        {
            var request = context.Request;
            
            // Read request body
            request.EnableBuffering();
            var requestBody = string.Empty;
            if (request.ContentLength > 0)
            {
                request.Body.Position = 0;
                using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
                requestBody = await reader.ReadToEndAsync();
                request.Body.Position = 0;
            }

            // Build request info
            var requestInfo = new
            {
                RequestId = requestId,
                Timestamp = DateTime.UtcNow,
                Method = request.Method,
                Path = request.Path.Value,
                QueryString = request.QueryString.Value,
                ContentType = request.ContentType,
                ContentLength = request.ContentLength,
                //Headers = request.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)),
                Body = ShouldLogBody(request) ? requestBody : "[BODY_NOT_LOGGED]",
                ClientIP = GetClientIpAddress(context),
                UserAgent = request.Headers.UserAgent.ToString()
            };

            _logger.LogInformation("🔵 REQUEST START: {RequestInfo}", 
                System.Text.Json.JsonSerializer.Serialize(requestInfo, new System.Text.Json.JsonSerializerOptions 
                { 
                    WriteIndented = true 
                }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging request for ID: {RequestId}", requestId);
        }
    }

    private async Task LogResponseAsync(HttpContext context, string requestId, long elapsedMs, MemoryStream responseBody)
    {
        try
        {
            var response = context.Response;
            
            // Read response body
            responseBody.Seek(0, SeekOrigin.Begin);
            var responseBodyText = string.Empty;
            if (responseBody.Length > 0)
            {
                using var reader = new StreamReader(responseBody, Encoding.UTF8, leaveOpen: true);
                responseBodyText = await reader.ReadToEndAsync();
            }

            // Build response info
            var responseInfo = new
            {
                RequestId = requestId,
                Timestamp = DateTime.UtcNow,
                StatusCode = response.StatusCode,
                StatusDescription = GetStatusDescription(response.StatusCode),
                ContentType = response.ContentType,
                ContentLength = response.ContentLength ?? responseBody.Length,
                //Headers = response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)),
                Body = ShouldLogResponseBody(response) ? responseBodyText : "[BODY_NOT_LOGGED]",
                ElapsedMs = elapsedMs
            };

            var logLevel = response.StatusCode >= 400 ? LogLevel.Warning : LogLevel.Information;
            var emoji = response.StatusCode switch
            {
                >= 200 and < 300 => "🟢",
                >= 300 and < 400 => "🟡",
                >= 400 and < 500 => "🟠",
                >= 500 => "🔴",
                _ => "⚪"
            };

            _logger.Log(logLevel, "{Emoji} RESPONSE END: {ResponseInfo}", 
                emoji, 
                System.Text.Json.JsonSerializer.Serialize(responseInfo, new System.Text.Json.JsonSerializerOptions 
                { 
                    WriteIndented = true 
                }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging response for ID: {RequestId}", requestId);
        }
    }

    private static bool ShouldLogBody(HttpRequest request)
    {
        // Don't log binary content or very large payloads
        if (request.ContentLength > 10_000) return false;
        if (string.IsNullOrEmpty(request.ContentType)) return false;
        
        var contentType = request.ContentType.ToLower();
        return contentType.Contains("json") || 
               contentType.Contains("xml") || 
               contentType.Contains("text") || 
               contentType.Contains("form");
    }

    private static bool ShouldLogResponseBody(HttpResponse response)
    {
        // Don't log binary content or very large payloads
        if (response.ContentLength > 10_000) return false;
        if (string.IsNullOrEmpty(response.ContentType)) return false;
        
        var contentType = response.ContentType.ToLower();
        return contentType.Contains("json") || 
               contentType.Contains("xml") || 
               contentType.Contains("text") || 
               contentType.Contains("html");
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        // Try to get real IP from various headers
        var ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (string.IsNullOrEmpty(ipAddress))
            ipAddress = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (string.IsNullOrEmpty(ipAddress))
            ipAddress = context.Connection.RemoteIpAddress?.ToString();
        
        return ipAddress ?? "Unknown";
    }

    private static string GetStatusDescription(int statusCode)
    {
        return statusCode switch
        {
            200 => "OK",
            201 => "Created",
            204 => "No Content",
            400 => "Bad Request",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Not Found",
            500 => "Internal Server Error",
            503 => "Service Unavailable",
            _ => "Unknown"
        };
    }
} 