using DataChat.Gateway.Configuration;
using Microsoft.Extensions.Options;

namespace DataChat.Gateway.Auth;

/// <summary>从 Gateway:Auth:Users 配置校验用户名密码（独立 Web / 开发联调）。</summary>
public sealed class LocalConfigAuthProvider : IHostAuthProvider
{
    private readonly GatewayAuthOptions _options;

    public LocalConfigAuthProvider(IOptions<GatewayOptions> options) =>
        _options = options.Value.Auth;

    public Task<AuthUser?> ValidateCredentialsAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            return Task.FromResult<AuthUser?>(null);

        var key = username.Trim();
        var entry = _options.Users.FirstOrDefault(u =>
        {
            var login = string.IsNullOrWhiteSpace(u.Login) ? u.UserId : u.Login!;
            return string.Equals(login, key, StringComparison.OrdinalIgnoreCase);
        });

        if (entry is null || entry.Password != password)
            return Task.FromResult<AuthUser?>(null);

        return Task.FromResult<AuthUser?>(new AuthUser
        {
            UserId = entry.UserId.Trim(),
            UserName = string.IsNullOrWhiteSpace(entry.UserName) ? entry.UserId.Trim() : entry.UserName.Trim()
        });
    }
}
