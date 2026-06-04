using SqlSugar;

namespace DataChat.Infrastructure.Persistence;

[SugarTable("chat_session")]
public sealed class ChatSessionEntity
{
    [SugarColumn(IsPrimaryKey = true, Length = 64)]
    public string Id { get; set; } = "";

    [SugarColumn(Length = 256, IsNullable = false)]
    public string Title { get; set; } = "";

    [SugarColumn(ColumnName = "domain_id", Length = 64, IsNullable = false)]
    public string DomainId { get; set; } = "";

    [SugarColumn(ColumnName = "chat_mode", Length = 64, IsNullable = false)]
    public string ChatMode { get; set; } = "";

    [SugarColumn(Length = 128, IsNullable = true)]
    public string? Model { get; set; }

    [SugarColumn(ColumnName = "resource_id", Length = 128, IsNullable = true)]
    public string? ResourceId { get; set; }

    [SugarColumn(ColumnName = "created_at")]
    public long CreatedAt { get; set; }

    [SugarColumn(ColumnName = "updated_at")]
    public long UpdatedAt { get; set; }
}
