using System.Text.Json.Serialization;

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
    /// <summary>旧版 dc_domain / 文件配置中的 ApiKeys 引用。</summary>
    public string ApiKeyRef { get; set; } = "coze-main";
    /// <summary>运行时从 dc_provider_account 注入，不序列化到配置 JSON。</summary>
    [JsonIgnore]
    public string? ApiKey { get; set; }
    /// <summary>API 域名，默认 api.coze.cn；国际版 api.coze.com。</summary>
    public string? Endpoint { get; set; }
    public bool AutoSaveHistory { get; set; } = true;
    /// <summary>传给 Coze UserID 的前缀，便于多租户隔离；最终为 {prefix}:{sessionId}。</summary>
    public string? UserIdPrefix { get; set; }
    public Dictionary<string, string>? CustomVariables { get; set; }
    /// <summary>扣子空间 ID（URL 中 space/ 后的数字），用于 GET /v1/workflows 拉取工作流列表。</summary>
    public string? WorkspaceId { get; set; }
    /// <summary>列表 API 可选：按 app_id 筛选（扣子应用内工作流）。</summary>
    public string? ListAppId { get; set; }
    /// <summary>列表 API 可选：published_online / all / published_draft 等，默认 published_online。</summary>
    public string? ListPublishStatus { get; set; }
    /// <summary>可选：预配置工作流；与 API 列表合并，本地项可覆盖 inputParameter 等。</summary>
    public List<CozeWorkflowOptions>? Workflows { get; set; }
}

public sealed class CozeWorkflowOptions
{
    public required string WorkflowId { get; init; }
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    /// <summary>用户输入映射到 workflow parameters 的键，默认 BOT_USER_INPUT。</summary>
    public string InputParameter { get; set; } = "BOT_USER_INPUT";
    /// <summary>给用户的简短提示，覆盖自动生成的 placeholder。</summary>
    public string? InputHint { get; set; }
    /// <summary>覆盖/补充 Coze API 拉取的开始节点参数说明。</summary>
    public List<CozeWorkflowInputOptions>? Inputs { get; set; }
    public Dictionary<string, string>? DefaultParameters { get; set; }
    /// <summary>扣子应用内工作流需指定 app_id（与 bot_id 勿同时传）。</summary>
    public string? AppId { get; set; }
}

public sealed class CozeWorkflowInputOptions
{
    public required string Name { get; init; }
    public string? Type { get; set; }
    public bool? Required { get; set; }
    public string? Label { get; set; }
    public string? Description { get; set; }
    public List<string>? Accept { get; set; }
}

public sealed class CustomDomainOptions
{
    public string Endpoint { get; set; } = "";
    public string ApiKeyRef { get; set; } = "default";
    [JsonIgnore]
    public string? ApiKey { get; set; }
    public string Adapter { get; set; } = "default";
    public string? AuthHeaderName { get; set; }
}

public sealed class OpenAiDomainOptions
{
    public string BaseUrl { get; set; } = "";
    public string ApiKeyRef { get; set; } = "default";
    [JsonIgnore]
    public string? ApiKey { get; set; }
}
