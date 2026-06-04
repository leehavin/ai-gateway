using DataChat.Core.Abstractions;
using DataChat.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

namespace DataChat.Infrastructure.Configuration;

public static class DomainsConfigurationRegistrar
{
    public static string ResolveDatabasePath(string databasePath, string baseDirectory)
    {
        if (Path.IsPathRooted(databasePath)) return databasePath;
        return Path.Combine(baseDirectory, databasePath);
    }

    public static string ResolveDomainsFilePath(string domainsFile, string baseDirectory)
    {
        if (Path.IsPathRooted(domainsFile)) return domainsFile;
        return Path.Combine(baseDirectory, domainsFile);
    }

    /// <summary>注册领域配置存储与 <see cref="IDomainCatalog"/>，返回启动快照供 Provider 注册使用。</summary>
    public static DomainsConfiguration RegisterDomains(
        IServiceCollection services,
        DomainsSetupOptions options,
        string baseDirectory,
        ISqlSugarClient? sharedDb = null)
    {
        IDomainsConfigurationStore store;

        if (options.Source == DomainsSource.Database)
        {
            var db = sharedDb ?? SqlSugarConnectionFactory.Create(
                new DataChatDbOptions { DatabasePath = options.DatabasePath },
                baseDirectory);
            if (sharedDb is null)
            {
                if (options.SeedFromFileWhenEmpty)
                {
                    var seedPath = ResolveDomainsFilePath(options.DomainsFile, baseDirectory);
                    DomainsDatabaseBootstrap.SeedIfEmptyAsync(db, seedPath).GetAwaiter().GetResult();
                }
                services.AddSingleton<ISqlSugarClient>(db);
            }

            store = new SqlSugarDomainsConfigurationStore(db);
        }
        else
        {
            store = new FileDomainsConfigurationStore(ResolveDomainsFilePath(options.DomainsFile, baseDirectory));
        }

        services.AddSingleton<IDomainsConfigurationStore>(store);

        var config = store.LoadAsync().GetAwaiter().GetResult();
        services.AddSingleton<IDomainCatalog>(new DomainCatalog(store, config));
        services.AddSingleton(config);
        return config;
    }
}
