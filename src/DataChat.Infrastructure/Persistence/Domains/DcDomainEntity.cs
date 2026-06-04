using SqlSugar;

namespace DataChat.Infrastructure.Persistence.Domains;

/// <summary>
/// 可扩展领域行：provider 决定 provider_options_json 的反序列化类型
/// （coze / dbgpt / custom / openai / 未来本地或在线模型适配器）。
/// </summary>
[SugarTable("dc_domain")]
public sealed class DcDomainEntity
{
    [SugarColumn(IsPrimaryKey = true, Length = 64)]
    public string Id { get; set; } = "";

    [SugarColumn(Length = 128, IsNullable = false)]
    public string DisplayName { get; set; } = "";

    [SugarColumn(Length = 64, IsNullable = false)]
    public string ChatMode { get; set; } = "";

    [SugarColumn(Length = 32, IsNullable = false)]
    public string Provider { get; set; } = "";

    [SugarColumn(Length = 128, IsNullable = true)]
    public string? Model { get; set; }

    [SugarColumn(ColumnDataType = "TEXT", IsNullable = true)]
    public string? SystemPrompt { get; set; }

    [SugarColumn(Length = 512, IsNullable = true)]
    public string? Placeholder { get; set; }

    [SugarColumn(ColumnDataType = "TEXT", IsNullable = true)]
    public string? QuickPromptsJson { get; set; }

    [SugarColumn(ColumnDataType = "TEXT", IsNullable = true)]
    public string? ProviderOptionsJson { get; set; }

    public int SortOrder { get; set; }

    public bool Enabled { get; set; } = true;

    public long UpdatedAt { get; set; }
}
