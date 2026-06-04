using DataChat.Core.Configuration;
using DataChat.Infrastructure.Persistence.Domains;
using SqlSugar;

namespace DataChat.Infrastructure.Configuration;

public static class DomainsDatabaseBootstrap
{
    public static async Task SeedIfEmptyAsync(
        ISqlSugarClient db,
        string seedFilePath,
        CancellationToken cancellationToken = default)
    {
        var count = await db.Queryable<DcDomainEntity>().CountAsync(cancellationToken);
        if (count > 0) return;

        if (!File.Exists(seedFilePath))
            throw new FileNotFoundException($"领域种子文件不存在: {seedFilePath}");

        var seed = DomainsConfigurationLoader.Load(seedFilePath);
        await ImportAsync(db, seed, cancellationToken);
    }

    public static async Task ImportAsync(
        ISqlSugarClient db,
        DomainsConfiguration config,
        CancellationToken cancellationToken = default)
    {
        var defaults = DomainEntityMapper.ToEntity(config.Defaults, config.Version);
        await db.Storageable(defaults).WhereColumns(x => x.Id).ExecuteCommandAsync(cancellationToken);

        var rows = config.Domains
            .Select((d, i) => DomainEntityMapper.ToEntity(d, i))
            .ToList();

        if (rows.Count == 0) return;

        await db.Storageable(rows).WhereColumns(x => x.Id).ExecuteCommandAsync(cancellationToken);
    }
}
