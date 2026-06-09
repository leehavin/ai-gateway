using SqlSugar;

namespace DataChat.Infrastructure.Persistence.Access;

[SugarTable("dc_user_agent")]
public sealed class DcUserAgentEntity
{
    [SugarColumn(ColumnName = "user_id", IsPrimaryKey = true)]
    public long UserId { get; set; }

    [SugarColumn(ColumnName = "agent_id", IsPrimaryKey = true, Length = 64)]
    public string AgentId { get; set; } = "";
}
