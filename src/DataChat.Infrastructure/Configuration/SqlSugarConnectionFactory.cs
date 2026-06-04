using Microsoft.Data.Sqlite;
using SqlSugar;

namespace DataChat.Infrastructure.Configuration;

public static class SqlSugarConnectionFactory
{
    public static ISqlSugarClient Create(DataChatDbOptions options, string baseDirectory)
    {
        var (connectionString, dbType) = Resolve(options, baseDirectory);
        return new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = connectionString,
            DbType = dbType,
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute,
            ConfigureExternalServices = new ConfigureExternalServices
            {
                EntityService = (_, column) =>
                {
                    if (dbType == DbType.SqlServer && column.IsPrimarykey && !column.IsIdentity)
                        column.IsIdentity = false;
                }
            }
        });
    }

    public static (string ConnectionString, DbType DbType) Resolve(DataChatDbOptions options, string baseDirectory)
    {
        if (!string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            var dbType = options.IsSqlServer ? DbType.SqlServer : DbType.Sqlite;
            return (options.ConnectionString.Trim(), dbType);
        }

        if (options.IsSqlServer)
            throw new InvalidOperationException("Provider=SqlServer 时必须配置 ConnectionStrings:DataChat 或 Gateway:ConnectionString。");

        var path = DomainsConfigurationRegistrar.ResolveDatabasePath(options.DatabasePath, baseDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var cs = new SqliteConnectionStringBuilder { DataSource = path }.ConnectionString;
        return (cs, DbType.Sqlite);
    }
}
