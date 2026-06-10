using DataChat.Gateway.Auth;
using DataChat.Gateway.Configuration;
using Microsoft.Extensions.Options;

namespace DataChat.Gateway.Middleware;

public sealed class GatewayAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly GatewayAuthService _auth;

    public GatewayAuthMiddleware(RequestDelegate next, GatewayAuthService auth)
    {
        _next = next;
        _auth = auth;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        if (HttpMethods.IsOptions(context.Request.Method) || IsAnonymousPath(path))
        {
            await _next(context);
            return;
        }

        if (!_auth.IsAuthEnabled)
        {
            await _next(context);
            return;
        }

        if (IsAuthEntryPath(path) && HttpMethods.IsPost(context.Request.Method))
        {
            await _next(context);
            return;
        }

        if (!TryValidateBearer(context.Request.Headers.Authorization.ToString(), out var user))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Unauthorized", message = "缺少或无效的 Bearer Token。" });
            return;
        }

        context.Items[GatewayAuthConstants.HttpContextUserKey] = user;
        await _next(context);
    }

    private bool TryValidateBearer(string authorization, out AuthUser user)
    {
        user = null!;
        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return false;

        var token = authorization["Bearer ".Length..].Trim();
        var validated = _auth.ValidateBearer(token);
        if (validated is null)
            return false;

        user = validated;
        return true;
    }

    private static bool IsAnonymousPath(string path) =>
        path.StartsWith("/v1/health", StringComparison.OrdinalIgnoreCase)
        || path.Equals("/health", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase);

    /// <summary>无需 Bearer；由控制器内 ServiceKey 等机制鉴权（宿主文件登记等）。</summary>
    private static bool IsAuthEntryPath(string path) =>
        path.Equals("/v1/auth/login", StringComparison.OrdinalIgnoreCase)
        || path.Equals("/v1/auth/token", StringComparison.OrdinalIgnoreCase)
        || path.Equals("/v1/files/register", StringComparison.OrdinalIgnoreCase);
}
