using System.Net;
using System.Text.Json;

namespace DataChat.Gateway.Middleware;

public sealed class GatewayExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GatewayExceptionMiddleware> _logger;

    public GatewayExceptionMiddleware(RequestDelegate next, ILogger<GatewayExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation("请求已取消 {Path}", context.Request.Path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "网关未处理异常 {Path}", context.Request.Path);
            if (context.Response.HasStarted) throw;

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                error = "InternalServerError",
                message = "服务暂时不可用，请稍后重试。"
            }));
        }
    }
}
