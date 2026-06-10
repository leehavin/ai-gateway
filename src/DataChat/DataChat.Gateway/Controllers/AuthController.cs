using DataChat.Gateway.Auth;
using DataChat.Gateway.Models;
using Microsoft.AspNetCore.Mvc;

namespace DataChat.Gateway.Controllers;

[ApiController]
[Route("v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly GatewayAuthService _auth;
    private readonly ICurrentUserService _currentUser;

    public AuthController(GatewayAuthService auth, ICurrentUserService currentUser)
    {
        _auth = auth;
        _currentUser = currentUser;
    }

    /// <summary>独立 Web：用户名密码登录，返回用户会话 Token。</summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrEmpty(request.Password))
            return BadRequest(new { message = "username 与 password 不能为空。" });

        var result = await _auth.LoginAsync(request.Username.Trim(), request.Password, cancellationToken);
        if (result is null)
            return Unauthorized(new { message = "用户名或密码错误。" });

        return Ok(ToResponse(result));
    }

    /// <summary>WinForms / 宿主嵌入：用 ServiceKey 为已登录用户换取会话 Token。</summary>
    [HttpPost("token")]
    public async Task<ActionResult<AuthResponse>> IssueToken(
        [FromBody] IssueTokenRequest request,
        CancellationToken cancellationToken)
    {
        var serviceKey = ResolveServiceKey(request.ServiceKey);
        if (string.IsNullOrWhiteSpace(serviceKey))
            return Unauthorized(new { message = "缺少或无效的服务密钥。" });

        var result = await _auth.IssueTrustedUserAsync(
            serviceKey,
            request.UserId,
            request.UserName,
            cancellationToken);
        if (result is null)
            return Unauthorized(new { message = "无法签发用户 Token，请检查 ServiceKey、userId，或确认 sys_user 中存在对应登录账号。" });

        return Ok(ToResponse(result));
    }

    /// <summary>当前 Bearer Token 对应的用户信息。</summary>
    [HttpGet("me")]
    public ActionResult<AuthMeResponse> Me()
    {
        var user = _currentUser.Current;
        if (user is null)
            return Unauthorized(new { message = "未登录或 Token 无效。" });

        return Ok(new AuthMeResponse
        {
            UserId = user.UserId,
            UserName = user.UserName,
            IsSharedToken = user.IsSharedToken
        });
    }

    private static AuthResponse ToResponse(AuthResult result) => new()
    {
        Token = result.Token,
        UserId = result.UserId,
        UserName = result.UserName,
        ExpiresAt = result.ExpiresAt.ToUnixTimeMilliseconds()
    };

    private string? ResolveServiceKey(string? bodyKey)
    {
        if (!string.IsNullOrWhiteSpace(bodyKey))
            return bodyKey.Trim();

        var header = Request.Headers.Authorization.ToString();
        if (header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return header["Bearer ".Length..].Trim();

        return null;
    }
}
