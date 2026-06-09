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

    /// <summary>用户认证（独立 Web 登录 / WinForms 宿主换 Token）。</summary>
    public GatewayAuthOptions Auth { get; set; } = new();
}

public sealed class GatewayAuthOptions
{
    /// <summary>HMAC 签名密钥，用于签发与校验用户会话 Token。</summary>
    public string SigningKey { get; set; } = "";

    public int TokenLifetimeHours { get; set; } = 24;

    /// <summary>WinForms / 宿主系统用服务密钥换取用户 Token（POST /v1/auth/token）。</summary>
    public string? ServiceKey { get; set; }

    /// <summary>本地用户表（可替换为 IHostAuthProvider 其他实现）。</summary>
    public LocalAuthUserEntry[] Users { get; set; } = [];
}

public sealed class LocalAuthUserEntry
{
    /// <summary>稳定用户 ID，用于会话隔离。</summary>
    public string UserId { get; set; } = "";

    /// <summary>登录名；为空时使用 UserId。</summary>
    public string? Login { get; set; }

    /// <summary>界面显示名。</summary>
    public string UserName { get; set; } = "";

    public string Password { get; set; } = "";
}
