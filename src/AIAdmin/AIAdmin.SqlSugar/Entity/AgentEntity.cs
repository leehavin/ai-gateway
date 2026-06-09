namespace AIAdmin.SqlSugar.Entity;

/// <summary>
/// 智能体（Gateway domain id）
/// </summary>
[SugarTable("dc_agent")]
public partial class AgentEntity
{
    [SugarColumn(ColumnName = "id", IsPrimaryKey = true)]
    public string Id { get; set; } = "";

    [SugarColumn(ColumnName = "display_name")]
    public string DisplayName { get; set; } = "";

    [SugarColumn(ColumnName = "provider")]
    public string Provider { get; set; } = "";

    [SugarColumn(ColumnName = "chat_mode")]
    public string ChatMode { get; set; } = "";

    [SugarColumn(ColumnName = "provider_account_id")]
    public long? ProviderAccountId { get; set; }

    [SugarColumn(ColumnName = "model")]
    public string? Model { get; set; }

    [SugarColumn(ColumnName = "config_json")]
    public string ConfigJson { get; set; } = "{}";

    [SugarColumn(ColumnName = "placeholder")]
    public string? Placeholder { get; set; }

    [SugarColumn(ColumnName = "quick_prompts_json")]
    public string? QuickPromptsJson { get; set; }

    [SugarColumn(ColumnName = "system_prompt")]
    public string? SystemPrompt { get; set; }

    [SugarColumn(ColumnName = "sort_order")]
    public int SortOrder { get; set; }

    [SugarColumn(ColumnName = "enabled")]
    public bool Enabled { get; set; } = true;

    [SugarColumn(ColumnName = "remark")]
    public string? Remark { get; set; }

    [SugarColumn(ColumnName = "create_by")]
    public long? CreateBy { get; set; }

    [SugarColumn(ColumnName = "create_time")]
    public DateTime CreateTime { get; set; }

    [SugarColumn(ColumnName = "update_by")]
    public long? UpdateBy { get; set; }

    [SugarColumn(ColumnName = "update_time")]
    public DateTime? UpdateTime { get; set; }

    [SugarColumn(IsIgnore = true)]
    public string? ProviderAccountName { get; set; }
}
