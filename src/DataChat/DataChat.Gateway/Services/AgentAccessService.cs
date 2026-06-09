using DataChat.Gateway.Auth;
using DataChat.Infrastructure.Persistence.Access;
using DataChat.Infrastructure.Persistence.Agents;
using SqlSugar;

namespace DataChat.Gateway.Services;

public interface IAgentAccessService
{
    /// <summary>null 表示不过滤（超管、共享 Token 或未启用鉴权）。</summary>
    Task<IReadOnlySet<string>?> GetAllowedAgentIdsAsync(AuthUser? user, CancellationToken cancellationToken);

    /// <summary>null 表示继承智能体下全部工作流；空集表示无权限。</summary>
    Task<IReadOnlySet<string>?> GetAllowedWorkflowIdsAsync(
        AuthUser? user,
        string agentId,
        CancellationToken cancellationToken);
}

/// <summary>无数据库时不过滤智能体列表。</summary>
public sealed class UnrestrictedAgentAccessService : IAgentAccessService
{
    public Task<IReadOnlySet<string>?> GetAllowedAgentIdsAsync(AuthUser? user, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlySet<string>?>(null);

    public Task<IReadOnlySet<string>?> GetAllowedWorkflowIdsAsync(
        AuthUser? user,
        string agentId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlySet<string>?>(null);
}

public sealed class AgentAccessService : IAgentAccessService
{
    private const string SuperAdminRoleName = "超级管理员";

    private readonly ISqlSugarClient _db;

    public AgentAccessService(ISqlSugarClient db) => _db = db;

    public async Task<IReadOnlySet<string>?> GetAllowedAgentIdsAsync(
        AuthUser? user,
        CancellationToken cancellationToken)
    {
        if (user is null || user.IsSharedToken)
            return null;

        if (!long.TryParse(user.UserId, out var userId))
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (await IsSuperAdminAsync(userId, cancellationToken))
            return null;

        var roleIds = await GetRoleIdsAsync(userId, cancellationToken);
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (roleIds.Count > 0)
        {
            var fromRoles = await _db.Queryable<DcRoleAgentEntity>()
                .Where(x => roleIds.Contains(x.RoleId))
                .Select(x => x.AgentId)
                .ToListAsync(cancellationToken);
            foreach (var id in fromRoles)
                allowed.Add(id);
        }

        var fromUser = await _db.Queryable<DcUserAgentEntity>()
            .Where(x => x.UserId == userId)
            .Select(x => x.AgentId)
            .ToListAsync(cancellationToken);
        foreach (var id in fromUser)
            allowed.Add(id);

        return allowed;
    }

    public async Task<IReadOnlySet<string>?> GetAllowedWorkflowIdsAsync(
        AuthUser? user,
        string agentId,
        CancellationToken cancellationToken)
    {
        var allowedAgents = await GetAllowedAgentIdsAsync(user, cancellationToken);
        if (allowedAgents is not null && !allowedAgents.Contains(agentId))
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (user is null || user.IsSharedToken)
            return null;

        if (!long.TryParse(user.UserId, out var userId))
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (await IsSuperAdminAsync(userId, cancellationToken))
            return null;

        var roleIds = await GetRoleIdsAsync(userId, cancellationToken);
        if (roleIds.Count == 0)
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var resourceRowIds = await _db.Queryable<DcRoleResourceEntity>()
            .Where(x => roleIds.Contains(x.RoleId))
            .Select(x => x.ResourceRowId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (resourceRowIds.Count == 0)
            return null;

        var workflows = await _db.Queryable<DcAgentResourceEntity>()
            .Where(x => x.AgentId == agentId
                && x.Enabled
                && x.ResourceType == "workflow"
                && resourceRowIds.Contains(x.Id))
            .Select(x => x.ExternalId)
            .ToListAsync(cancellationToken);

        return new HashSet<string>(workflows, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<bool> IsSuperAdminAsync(long userId, CancellationToken cancellationToken)
    {
        try
        {
            return await _db.Queryable<SysUserRoleEntity, SysRoleEntity>((ur, r) =>
                    ur.RoleId == r.Id)
                .Where((ur, r) => ur.UserId == userId && r.Name == SuperAdminRoleName)
                .AnyAsync(cancellationToken);
        }
        catch
        {
            return false;
        }
    }

    private async Task<List<long>> GetRoleIdsAsync(long userId, CancellationToken cancellationToken)
    {
        try
        {
            return await _db.Queryable<SysUserRoleEntity>()
                .Where(x => x.UserId == userId)
                .Select(x => x.RoleId)
                .ToListAsync(cancellationToken);
        }
        catch
        {
            return [];
        }
    }
}
