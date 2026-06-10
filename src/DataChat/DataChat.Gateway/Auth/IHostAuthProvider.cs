namespace DataChat.Gateway.Auth;

/// <summary>宿主用户认证适配器（IPSpace、LDAP、OAuth 等可各自实现）。</summary>
public interface IHostAuthProvider
{
    Task<AuthUser?> ValidateCredentialsAsync(string username, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// 宿主嵌入换 Token：优先按登录账号解析为 AIAdmin <c>sys_user</c>，供智能体权限过滤。
    /// </summary>
    Task<AuthUser?> ResolveEmbeddedUserAsync(
        string? userId,
        string? userName,
        CancellationToken cancellationToken = default);
}
