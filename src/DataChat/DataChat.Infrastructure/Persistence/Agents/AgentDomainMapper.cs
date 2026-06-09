using System.Text.Json;
using DataChat.Core.Configuration;

namespace DataChat.Infrastructure.Persistence.Agents;

internal static class AgentDomainMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static DomainProfile ToProfile(
        DcAgentEntity agent,
        DcProviderAccountEntity? account,
        IReadOnlyList<DcAgentResourceEntity> resources,
        GlobalDefaults defaults)
    {
        var provider = agent.Provider.Trim().ToLowerInvariant();
        var profile = new DomainProfile
        {
            Id = agent.Id,
            DisplayName = agent.DisplayName,
            ChatMode = agent.ChatMode,
            Provider = provider,
            Model = agent.Model,
            SystemPrompt = agent.SystemPrompt,
            Placeholder = agent.Placeholder,
            QuickPrompts = DeserializeQuickPrompts(agent.QuickPromptsJson)
        };

        var apiKey = account is { Enabled: true } ? account.ApiKeyCiphertext?.Trim() : null;
        var endpoint = account is { Enabled: true } ? account.Endpoint?.Trim() : null;

        switch (provider)
        {
            case "coze":
                profile.Coze = BuildCozeOptions(agent.ConfigJson, resources, apiKey, endpoint, defaults);
                break;
            case "dbgpt":
                profile.Dbgpt = DeserializeOrDefault<DbgptDomainOptions>(agent.ConfigJson) ?? new DbgptDomainOptions();
                break;
            case "custom":
                profile.Custom = BuildCustomOptions(agent.ConfigJson, apiKey, endpoint);
                break;
            case "openai":
                profile.OpenAi = BuildOpenAiOptions(agent.ConfigJson, apiKey, endpoint);
                break;
        }

        return profile;
    }

    private static CozeDomainOptions BuildCozeOptions(
        string configJson,
        IReadOnlyList<DcAgentResourceEntity> resources,
        string? apiKey,
        string? endpoint,
        GlobalDefaults defaults)
    {
        var coze = DeserializeOrDefault<CozeDomainOptions>(configJson)
            ?? throw new InvalidOperationException("Coze 智能体 config_json 无效。");

        if (string.IsNullOrWhiteSpace(coze.BotId))
            throw new InvalidOperationException("Coze 智能体缺少 botId。");

        coze.ApiKey = apiKey;
        coze.Endpoint = string.IsNullOrWhiteSpace(endpoint) ? coze.Endpoint ?? defaults.CozeEndpoint : endpoint;

        var workflows = resources
            .Where(r => r.Enabled
                && r.ResourceType.Equals("workflow", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(r.ExternalId))
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.DisplayName)
            .Select(ToWorkflowOptions)
            .ToList();

        if (workflows.Count > 0)
            coze.Workflows = workflows;

        return coze;
    }

    private static CozeWorkflowOptions ToWorkflowOptions(DcAgentResourceEntity resource)
    {
        var config = DeserializeOrDefault<CozeWorkflowResourceConfig>(resource.ConfigJson);
        return new CozeWorkflowOptions
        {
            WorkflowId = resource.ExternalId.Trim(),
            DisplayName = resource.DisplayName,
            Description = resource.Description,
            InputParameter = config?.InputParameter ?? "BOT_USER_INPUT",
            InputHint = config?.InputHint,
            Inputs = config?.Inputs,
            DefaultParameters = config?.DefaultParameters,
            AppId = config?.AppId
        };
    }

    private static CustomDomainOptions BuildCustomOptions(string configJson, string? apiKey, string? endpoint)
    {
        var custom = DeserializeOrDefault<CustomDomainOptions>(configJson) ?? new CustomDomainOptions();
        if (string.IsNullOrWhiteSpace(custom.Endpoint) && !string.IsNullOrWhiteSpace(endpoint))
            custom.Endpoint = endpoint;
        if (string.IsNullOrWhiteSpace(custom.Endpoint))
            throw new InvalidOperationException("Custom 智能体缺少 endpoint。");

        custom.ApiKey = apiKey;
        return custom;
    }

    private static OpenAiDomainOptions BuildOpenAiOptions(string configJson, string? apiKey, string? endpoint)
    {
        var openAi = DeserializeOrDefault<OpenAiDomainOptions>(configJson) ?? new OpenAiDomainOptions();
        if (string.IsNullOrWhiteSpace(openAi.BaseUrl) && !string.IsNullOrWhiteSpace(endpoint))
            openAi.BaseUrl = endpoint;
        if (string.IsNullOrWhiteSpace(openAi.BaseUrl))
            throw new InvalidOperationException("OpenAI 智能体缺少 baseUrl。");

        openAi.ApiKey = apiKey;
        return openAi;
    }

    private static T? DeserializeOrDefault<T>(string? json) where T : class
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private static List<string>? DeserializeQuickPrompts(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private sealed class CozeWorkflowResourceConfig
    {
        public string? InputParameter { get; set; }
        public string? InputHint { get; set; }
        public List<CozeWorkflowInputOptions>? Inputs { get; set; }
        public Dictionary<string, string>? DefaultParameters { get; set; }
        public string? AppId { get; set; }
    }
}
