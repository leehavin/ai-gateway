using DataChat.Core.Abstractions;
using DataChat.Core.Configuration;
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

        var domainRows = (await _db.Queryable<DcDomainEntity>()
            .Where(x => x.Enabled)
            .ToListAsync(cancellationToken))
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.DisplayName)
            .ToList();

        return new DomainsConfiguration
        {
            Version = defaultsRow?.Version ?? 1,
            Defaults = DomainEntityMapper.ToDefaults(defaultsRow),
            Domains = domainRows.Select(DomainEntityMapper.ToProfile).ToList()
        };
    }
}
