using System.Security.Cryptography;
using System.Text;
using DataChat.Infrastructure.Persistence.Access;
using SqlSugar;

namespace DataChat.Gateway.Auth;

/// <summary>
/// 与 AIAdmin 共用 <c>sys_user</c> 表（账号 + MD5 密码），签发 numeric userId 供智能体权限过滤。
/// </summary>
public sealed class SqlSugarHostAuthProvider : IHostAuthProvider
{
    private const int UserStatusNormal = 0;

    private readonly ISqlSugarClient _db;

    public SqlSugarHostAuthProvider(ISqlSugarClient db) => _db = db;

    public async Task<AuthUser?> ResolveEmbeddedUserAsync(
        string? userId,
        string? userName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(userName))
            {
                var key = userName.Trim();
                var byAccount = await _db.Queryable<SysUserEntity>()
                    .Where(x => x.Account == key)
                    .FirstAsync(cancellationToken);
                if (byAccount is not null && byAccount.Status == UserStatusNormal)
                    return ToAuthUser(byAccount);

                // IPSpace 等宿主常把显示名放在 s_UserName，与 sys_user.name 一致时也可解析。
                var byName = await _db.Queryable<SysUserEntity>()
                    .Where(x => x.Name == key)
                    .FirstAsync(cancellationToken);
                if (byName is not null && byName.Status == UserStatusNormal)
                    return ToAuthUser(byName);
            }

            if (long.TryParse(userId?.Trim(), out var numericId))
            {
                var byId = await _db.Queryable<SysUserEntity>()
                    .Where(x => x.Id == numericId)
                    .FirstAsync(cancellationToken);
                if (byId is not null && byId.Status == UserStatusNormal)
                    return ToAuthUser(byId);
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    public async Task<AuthUser?> ValidateCredentialsAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            return null;

        try
        {
            var account = username.Trim();
            var hash = Md5Hex(password);
            var user = await _db.Queryable<SysUserEntity>()
                .Where(x => x.Account == account && x.Password == hash)
                .FirstAsync(cancellationToken);

            if (user is null || user.Status != UserStatusNormal)
                return null;

            return ToAuthUser(user);
        }
        catch
        {
            return null;
        }
    }

    private static AuthUser ToAuthUser(SysUserEntity user) => new()
    {
        UserId = user.Id.ToString(),
        UserName = string.IsNullOrWhiteSpace(user.Name) ? user.Account : user.Name.Trim()
    };

    private static string Md5Hex(string text)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
