using SqlSugar;

namespace DataChat.Infrastructure.Persistence.Access;

[SugarTable("dc_role_agent")]
public sealed class DcRoleAgentEntity
{
    [SugarColumn(ColumnName = "role_id", IsPrimaryKey = true)]
    public long RoleId { get; set; }

    [SugarColumn(ColumnName = "agent_id", IsPrimaryKey = true, Length = 64)]
    public string AgentId { get; set; } = "";
}
