using SqlSugar;

namespace DataChat.Infrastructure.Persistence.Domains;

[SugarTable("dc_domain")]
public sealed class DcDomainEntity
{
    [SugarColumn(IsPrimaryKey = true, Length = 64)]
    public string Id { get; set; } = "";

    [SugarColumn(ColumnName = "display_name", Length = 128, IsNullable = false)]
    public string DisplayName { get; set; } = "";

    [SugarColumn(ColumnName = "chat_mode", Length = 64, IsNullable = false)]
    public string ChatMode { get; set; } = "";

    [SugarColumn(Length = 32, IsNullable = false)]
    public string Provider { get; set; } = "";

    [SugarColumn(Length = 128, IsNullable = true)]
    public string? Model { get; set; }

    [SugarColumn(ColumnName = "system_prompt", ColumnDataType = "TEXT", IsNullable = true)]
    public string? SystemPrompt { get; set; }

    [SugarColumn(Length = 512, IsNullable = true)]
    public string? Placeholder { get; set; }

    [SugarColumn(ColumnName = "quick_prompts_json", ColumnDataType = "TEXT", IsNullable = true)]
    public string? QuickPromptsJson { get; set; }

    [SugarColumn(ColumnName = "provider_options_json", ColumnDataType = "TEXT", IsNullable = true)]
    public string? ProviderOptionsJson { get; set; }

    [SugarColumn(ColumnName = "sort_order")]
    public int SortOrder { get; set; }

    public bool Enabled { get; set; } = true;

    [SugarColumn(ColumnName = "updated_at")]
    public long UpdatedAt { get; set; }
}
