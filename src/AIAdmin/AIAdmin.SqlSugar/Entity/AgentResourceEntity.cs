namespace AIAdmin.SqlSugar.Entity;

/// <summary>
/// 智能体子资源（workflow / tool / skill）
/// </summary>
[SugarTable("dc_agent_resource")]
public partial class AgentResourceEntity : BaseEntity
{
    [SugarColumn(ColumnName = "agent_id")]
    public string AgentId { get; set; } = "";

    [SugarColumn(ColumnName = "resource_type")]
    public string ResourceType { get; set; } = "workflow";

    [SugarColumn(ColumnName = "external_id")]
    public string ExternalId { get; set; } = "";

    [SugarColumn(ColumnName = "display_name")]
    public string? DisplayName { get; set; }

    [SugarColumn(ColumnName = "description")]
    public string? Description { get; set; }

    [SugarColumn(ColumnName = "config_json")]
    public string? ConfigJson { get; set; }

    [SugarColumn(ColumnName = "sort_order")]
    public int SortOrder { get; set; }

    [SugarColumn(ColumnName = "enabled")]
    public bool Enabled { get; set; } = true;
}
