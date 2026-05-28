using DataChat.Gateway.Configuration;
using Microsoft.Extensions.Options;

namespace DataChat.Gateway.Middleware;

public sealed class GatewayAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly GatewayOptions _options;

    public GatewayAuthMiddleware(RequestDelegate next, IOptions<GatewayOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        if (HttpMethods.IsOptions(context.Request.Method) || IsAnonymousPath(path))
        {
            await _next(context);
            return;
        }

        if (_options.ValidTokens.Length == 0)
        {
            await _next(context);
            return;
        }

        if (!TryValidateBearer(context.Request.Headers.Authorization.ToString(), out _))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Unauthorized", message = "缺少或无效的 Bearer Token。" });
            return;
        }

        await _next(context);
    }

    private bool TryValidateBearer(string authorization, out string token)
    {
        token = "";
        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return false;
        token = authorization["Bearer ".Length..].Trim();
        return !string.IsNullOrEmpty(token) && _options.ValidTokens.Contains(token);
    }

    private static bool IsAnonymousPath(string path) =>
        path.StartsWith("/v1/health", StringComparison.OrdinalIgnoreCase)
        || path.Equals("/health", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase);
}
