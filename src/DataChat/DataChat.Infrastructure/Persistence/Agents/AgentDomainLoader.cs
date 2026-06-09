using DataChat.Core.Configuration;
using DataChat.Infrastructure.Persistence.Domains;
using SqlSugar;

namespace DataChat.Infrastructure.Persistence.Agents;

internal static class AgentDomainLoader
{
    public static async Task<bool> HasEnabledAgentsAsync(ISqlSugarClient db, CancellationToken cancellationToken)
    {
        try
        {
            return await db.Queryable<DcAgentEntity>().AnyAsync(x => x.Enabled, cancellationToken);
        }
        catch
        {
            return false;
        }
    }

    public static async Task<List<DomainProfile>> LoadAsync(
        ISqlSugarClient db,
        GlobalDefaults defaults,
        CancellationToken cancellationToken = default)
    {
        var agents = (await db.Queryable<DcAgentEntity>()
                .Where(x => x.Enabled)
                .ToListAsync(cancellationToken))
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.DisplayName)
            .ToList();

        if (agents.Count == 0)
            return [];

        var accountIds = agents
            .Where(a => a.ProviderAccountId.HasValue)
            .Select(a => a.ProviderAccountId!.Value)
            .Distinct()
            .ToList();

        var accounts = accountIds.Count == 0
            ? new Dictionary<long, DcProviderAccountEntity>()
            : (await db.Queryable<DcProviderAccountEntity>()
                    .Where(x => accountIds.Contains(x.Id))
                    .ToListAsync(cancellationToken))
                .ToDictionary(x => x.Id);

        var agentIds = agents.Select(a => a.Id).ToList();
        var resources = (await db.Queryable<DcAgentResourceEntity>()
                .Where(x => agentIds.Contains(x.AgentId) && x.Enabled)
                .ToListAsync(cancellationToken))
            .GroupBy(x => x.AgentId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<DcAgentResourceEntity>)g.ToList());

        var profiles = new List<DomainProfile>(agents.Count);
        foreach (var agent in agents)
        {
            DcProviderAccountEntity? account = null;
            if (agent.ProviderAccountId is long accountId)
                accounts.TryGetValue(accountId, out account);

            resources.TryGetValue(agent.Id, out var agentResources);
            try
            {
                profiles.Add(AgentDomainMapper.ToProfile(
                    agent,
                    account,
                    agentResources ?? [],
                    defaults));
            }
            catch
            {
                // 单行损坏时跳过，避免整表加载失败
            }
        }

        return profiles;
    }
}
