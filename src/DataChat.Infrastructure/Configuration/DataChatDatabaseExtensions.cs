using DataChat.Core.Abstractions;
using DataChat.Core.Configuration;
using DataChat.Infrastructure.Persistence;
using DataChat.Infrastructure.Persistence.Domains;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

namespace DataChat.Infrastructure.Configuration;

public static class DataChatDatabaseExtensions
{
    public static DataChatDbOptions ReadDbOptions(IConfiguration configuration)
    {
        var conn =
            configuration.GetConnectionString("DataChat")
            ?? configuration["Gateway:ConnectionString"];

        var provider =
            configuration["Gateway:DbProvider"]
            ?? configuration["Database:Provider"]
            ?? (string.IsNullOrWhiteSpace(conn) ? "Sqlite" : "SqlServer");

        return new DataChatDbOptions
        {
            Provider = provider,
            ConnectionString = conn,
            DatabasePath = configuration["Gateway:DatabasePath"]
                ?? configuration["DatabasePath"]
                ?? "data/datachat.db"
        };
    }

    /// <summary>注册 SqlSugar（按需）与会话仓储；表结构须事先执行 db/ 下 SQL 脚本。</summary>
    public static ISqlSugarClient? AddDataChatDatabase(
        IServiceCollection services,
        DataChatDbOptions dbOptions,
        DomainsSetupOptions domainsSetup,
        string baseDirectory)
    {
        var useSqlSugar = domainsSetup.Source == DomainsSource.Database || dbOptions.IsSqlServer;

        ISqlSugarClient? db = null;
        if (useSqlSugar)
        {
            db = SqlSugarConnectionFactory.Create(dbOptions, baseDirectory);
            services.AddSingleton(db);

            if (domainsSetup.Source == DomainsSource.Database)
            {
                if (domainsSetup.SeedFromFileWhenEmpty)
                {
                    var seedPath = DomainsConfigurationRegistrar.ResolveDomainsFilePath(
                        domainsSetup.DomainsFile,
                        baseDirectory);
                    DomainsDatabaseBootstrap.SeedIfEmptyAsync(db, seedPath).GetAwaiter().GetResult();
                }
            }

            services.AddSingleton<IConversationRepository, SqlSugarConversationRepository>();
        }
        else
        {
            var path = DomainsConfigurationRegistrar.ResolveDatabasePath(dbOptions.DatabasePath, baseDirectory);
            services.AddSingleton<IConversationRepository>(_ => new SqliteConversationRepository(path));
        }

        return db;
    }
}
