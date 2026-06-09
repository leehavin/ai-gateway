using DataChat.Core.Abstractions;
using DataChat.Core.Configuration;
using DataChat.Infrastructure.Persistence.Agents;
using DataChat.Infrastructure.Persistence.Domains;
using SqlSugar;

namespace DataChat.Infrastructure.Configuration;

public sealed class SqlSugarDomainsConfigurationStore : IDomainsConfigurationStore
{
    private readonly ISqlSugarClient _db;

    public SqlSugarDomainsConfigurationStore(ISqlSugarClient db) => _db = db;

    public async Task<DomainsConfiguration> LoadAsync(CancellationToken cancellationToken = default)
    {
        var defaultsRow = await _db.Queryable<DcGlobalDefaultsEntity>()
            .Where(x => x.Id == 1)
            .FirstAsync(cancellationToken);

        var defaults = DomainEntityMapper.ToDefaults(defaultsRow);
        List<DomainProfile> domains;

        if (await AgentDomainLoader.HasEnabledAgentsAsync(_db, cancellationToken))
        {
            domains = await AgentDomainLoader.LoadAsync(_db, defaults, cancellationToken);
        }
        else
        {
            var domainRows = (await _db.Queryable<DcDomainEntity>()
                    .Where(x => x.Enabled)
                    .ToListAsync(cancellationToken))
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.DisplayName)
                .ToList();
            domains = domainRows.Select(DomainEntityMapper.ToProfile).ToList();
        }

        return new DomainsConfiguration
        {
            Version = defaultsRow?.Version ?? 1,
            Defaults = defaults,
            Domains = domains
        };
    }
}
