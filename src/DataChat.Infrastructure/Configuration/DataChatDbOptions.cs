namespace DataChat.Infrastructure.Configuration;

public sealed class DataChatDbOptions
{
    /// <summary>Sqlite 或 SqlServer。</summary>
    public string Provider { get; set; } = "Sqlite";

    /// <summary>完整连接串（优先于 DatabasePath）。</summary>
    public string? ConnectionString { get; set; }

    /// <summary>Sqlite 文件路径（Provider=Sqlite 且未配置 ConnectionString 时）。</summary>
    public string DatabasePath { get; set; } = "data/datachat.db";

    public bool IsSqlServer =>
        string.Equals(Provider, "SqlServer", StringComparison.OrdinalIgnoreCase)
        || (!string.IsNullOrWhiteSpace(ConnectionString)
            && ConnectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase));
}
