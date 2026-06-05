namespace DataChat.Gateway.Auth;

/// <summary>宿主用户认证适配器（IPSpace、LDAP、OAuth 等可各自实现）。</summary>
public interface IHostAuthProvider
{
    Task<AuthUser?> ValidateCredentialsAsync(string username, string password, CancellationToken cancellationToken = default);
}
