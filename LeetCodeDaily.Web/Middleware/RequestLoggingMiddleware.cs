using System.Diagnostics;
using Serilog.Context;

namespace LeetCodeDaily.Web.Middleware;

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
        var sw = Stopwatch.StartNew();
        var requestId = Activity.Current?.Id ?? context.TraceIdentifier;
        
        using (LogContext.PushProperty("RequestId", requestId))
        using (LogContext.PushProperty("RequestPath", context.Request.Path))
        using (LogContext.PushProperty("RequestMethod", context.Request.Method))
        {
            try
            {
                _logger.LogInformation("Request started: {Method} {Path}", 
                    context.Request.Method, 
                    context.Request.Path);

                await _next(context);

                sw.Stop();
                _logger.LogInformation("Request completed: {Method} {Path} - Status: {StatusCode} - Duration: {ElapsedMilliseconds}ms",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "Request failed: {Method} {Path} - Duration: {ElapsedMilliseconds}ms",
                    context.Request.Method,
                    context.Request.Path,
                    sw.ElapsedMilliseconds);
                throw;
            }
        }
    }
} 