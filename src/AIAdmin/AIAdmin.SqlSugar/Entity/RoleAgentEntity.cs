namespace AIAdmin.SqlSugar.Entity;

[SugarTable("dc_role_agent")]
public class RoleAgentEntity
{
    [SugarColumn(ColumnName = "role_id", IsPrimaryKey = true)]
    public long RoleId { get; set; }

    [SugarColumn(ColumnName = "agent_id", IsPrimaryKey = true)]
    public string AgentId { get; set; } = "";

    [SugarColumn(ColumnName = "create_by")]
    public long? CreateBy { get; set; }

    [SugarColumn(ColumnName = "create_time")]
    public DateTime CreateTime { get; set; }
}
