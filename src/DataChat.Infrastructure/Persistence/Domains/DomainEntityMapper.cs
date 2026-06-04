using System.Text.Json;
using DataChat.Core.Configuration;

namespace DataChat.Infrastructure.Persistence.Domains;

internal static class DomainEntityMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static DcGlobalDefaultsEntity ToEntity(GlobalDefaults d, int version = 1) => new()
    {
        Id = 1,
        Version = version,
        DbgptBaseUrl = d.DbgptBaseUrl,
        CozeEndpoint = d.CozeEndpoint,
        TimeoutSeconds = d.TimeoutSeconds,
        MaxHistoryTurns = d.MaxHistoryTurns,
        UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
    };

    public static GlobalDefaults ToDefaults(DcGlobalDefaultsEntity? row) =>
        row is null
            ? new GlobalDefaults()
            : new GlobalDefaults
            {
                DbgptBaseUrl = row.DbgptBaseUrl,
                CozeEndpoint = row.CozeEndpoint,
                TimeoutSeconds = row.TimeoutSeconds,
                MaxHistoryTurns = row.MaxHistoryTurns
            };

    public static DcDomainEntity ToEntity(DomainProfile profile, int sortOrder)
    {
        var provider = profile.Provider.Trim().ToLowerInvariant();
        return new DcDomainEntity
        {
            Id = profile.Id,
            DisplayName = profile.DisplayName,
            ChatMode = profile.ChatMode,
            Provider = provider,
            Model = profile.Model,
            SystemPrompt = profile.SystemPrompt,
            Placeholder = profile.Placeholder,
            QuickPromptsJson = profile.QuickPrompts is { Count: > 0 }
                ? JsonSerializer.Serialize(profile.QuickPrompts, JsonOptions)
                : null,
            ProviderOptionsJson = SerializeProviderOptions(profile, provider),
            SortOrder = sortOrder,
            Enabled = true,
            UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
    }

    public static DomainProfile ToProfile(DcDomainEntity row)
    {
        var provider = row.Provider.Trim().ToLowerInvariant();
        var profile = new DomainProfile
        {
            Id = row.Id,
            DisplayName = row.DisplayName,
            ChatMode = row.ChatMode,
            Provider = provider,
            Model = row.Model,
            SystemPrompt = row.SystemPrompt,
            Placeholder = row.Placeholder,
            QuickPrompts = DeserializeQuickPrompts(row.QuickPromptsJson)
        };
        ApplyProviderOptions(profile, provider, row.ProviderOptionsJson);
        return profile;
    }

    private static string? SerializeProviderOptions(DomainProfile profile, string provider)
    {
        object? payload = provider switch
        {
            "coze" => profile.Coze,
            "dbgpt" => profile.Dbgpt,
            "custom" => profile.Custom,
            "openai" => profile.OpenAi,
            _ => null
        };
        return payload is null ? null : JsonSerializer.Serialize(payload, JsonOptions);
    }

    private static void ApplyProviderOptions(DomainProfile profile, string provider, string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return;
        try
        {
            switch (provider)
            {
                case "coze":
                    profile.Coze = JsonSerializer.Deserialize<CozeDomainOptions>(json, JsonOptions);
                    break;
                case "dbgpt":
                    profile.Dbgpt = JsonSerializer.Deserialize<DbgptDomainOptions>(json, JsonOptions);
                    break;
                case "custom":
                    profile.Custom = JsonSerializer.Deserialize<CustomDomainOptions>(json, JsonOptions);
                    break;
                case "openai":
                    profile.OpenAi = JsonSerializer.Deserialize<OpenAiDomainOptions>(json, JsonOptions);
                    break;
            }
        }
        catch
        {
            // 单行配置损坏时跳过扩展块，避免整表加载失败
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
}
