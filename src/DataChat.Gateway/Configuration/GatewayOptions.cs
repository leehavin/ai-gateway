namespace DataChat.Gateway.Configuration;

public sealed class GatewayOptions
{
    public const string SectionName = "Gateway";

    public bool UseMock { get; set; }
    /// <summary>File 或 Database（Sqlite 表 dc_domain）。</summary>
    public string DomainsSource { get; set; } = "File";
    public string DomainsFile { get; set; } = "domains.json";
    /// <summary>数据库模式时从 DomainsFile 导入种子（表为空时）。</summary>
    public bool SeedDomainsFromFileWhenEmpty { get; set; }
    public string[] ValidTokens { get; set; } = [];
    public string[] AllowedOrigins { get; set; } = ["*"];
    public int TimeoutSeconds { get; set; } = 120;
    public int MaxMessageLength { get; set; } = 32000;
    public int MaxUploadSizeMb { get; set; } = 10;
    public int MaxUploadTextPreviewChars { get; set; } = 12000;
    public string UploadDirectory { get; set; } = "uploads";
    public string PathBase { get; set; } = "";
    public bool EnableSwagger { get; set; } = true;
    public string DatabasePath { get; set; } = "data/datachat.db";

    /// <summary>Sqlite 或 SqlServer；也可用 ConnectionStrings:DataChat 自动识别。</summary>
    public string DbProvider { get; set; } = "Sqlite";

    /// <summary>可选；优先使用 ConnectionStrings:DataChat。</summary>
    public string? ConnectionString { get; set; }
    public bool PersistSessions { get; set; } = true;
    public bool EnableSessionApi { get; set; } = true;
}
