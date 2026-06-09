using SqlSugar;

namespace DataChat.Infrastructure.Persistence.Agents;

[SugarTable("dc_agent_resource")]
public sealed class DcAgentResourceEntity
{
    [SugarColumn(IsPrimaryKey = true)]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "agent_id", Length = 64, IsNullable = false)]
    public string AgentId { get; set; } = "";

    [SugarColumn(ColumnName = "resource_type", Length = 32, IsNullable = false)]
    public string ResourceType { get; set; } = "workflow";

    [SugarColumn(ColumnName = "external_id", Length = 128, IsNullable = false)]
    public string ExternalId { get; set; } = "";

    [SugarColumn(ColumnName = "display_name", Length = 256, IsNullable = true)]
    public string? DisplayName { get; set; }

    [SugarColumn(Length = 2000, IsNullable = true)]
    public string? Description { get; set; }

    [SugarColumn(ColumnName = "config_json", ColumnDataType = "TEXT", IsNullable = true)]
    public string? ConfigJson { get; set; }

    [SugarColumn(ColumnName = "sort_order")]
    public int SortOrder { get; set; }

    public bool Enabled { get; set; } = true;
}
