using System.Diagnostics;
using Serilog.Context;

namespace LeetCodeDaily.Web.Middleware;

public class RequestIdMiddleware
{
    private readonly RequestDelegate _next;

    public RequestIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = Activity.Current?.Id ?? context.TraceIdentifier;
        context.Items["RequestId"] = requestId;
        
        using (LogContext.PushProperty("RequestId", requestId))
        {
            await _next(context);
        }
    }
} 