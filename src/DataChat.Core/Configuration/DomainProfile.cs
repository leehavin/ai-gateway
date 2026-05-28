namespace DataChat.Core.Configuration;

public sealed class DomainsConfiguration
{
    public int Version { get; set; } = 1;
    public GlobalDefaults Defaults { get; set; } = new();
    public List<DomainProfile> Domains { get; set; } = [];
}

public sealed class GlobalDefaults
{
    public string DbgptBaseUrl { get; set; } = "https://dbgpt.corp.example.com";
    public string CozeEndpoint { get; set; } = "api.coze.cn";
    public int TimeoutSeconds { get; set; } = 120;
    public int MaxHistoryTurns { get; set; } = 20;
}

public sealed class DomainProfile
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required string ChatMode { get; init; }
    public required string Provider { get; init; }
    public string? Model { get; set; }
    public string? SystemPrompt { get; set; }
    public string? Placeholder { get; set; }
    public List<string>? QuickPrompts { get; set; }
    public DbgptDomainOptions? Dbgpt { get; set; }
    public CozeDomainOptions? Coze { get; set; }
    public CustomDomainOptions? Custom { get; set; }
    public OpenAiDomainOptions? OpenAi { get; set; }
}

public sealed class DbgptDomainOptions
{
    public string ChatMode { get; set; } = "chat_app";
    public string? AppId { get; set; }
    public string? DatasourceId { get; set; }
    public string? KnowledgeSpaceName { get; set; }
}

public sealed class CozeDomainOptions
{
    /// <summary>Coze Bot ID（开发页 URL 中 bot 后的数字）。</summary>
    public required string BotId { get; init; }
    public string ApiKeyRef { get; set; } = "coze-main";
    /// <summary>API 域名，默认 api.coze.cn；国际版 api.coze.com。</summary>
    public string? Endpoint { get; set; }
    public bool AutoSaveHistory { get; set; } = true;
    /// <summary>传给 Coze UserID 的前缀，便于多租户隔离；最终为 {prefix}:{sessionId}。</summary>
    public string? UserIdPrefix { get; set; }
    public Dictionary<string, string>? CustomVariables { get; set; }
}

public sealed class CustomDomainOptions
{
    public required string Endpoint { get; init; }
    public string ApiKeyRef { get; set; } = "default";
    public string Adapter { get; set; } = "default";
    public string? AuthHeaderName { get; set; }
}

public sealed class OpenAiDomainOptions
{
    public required string BaseUrl { get; init; }
    public string ApiKeyRef { get; set; } = "default";
}
