using SqlSugar;

namespace DataChat.Infrastructure.Persistence;

[SugarTable("chat_message")]
public sealed class ChatMessageEntity
{
    [SugarColumn(IsPrimaryKey = true, Length = 64)]
    public string Id { get; set; } = "";

    [SugarColumn(ColumnName = "session_id", Length = 64, IsNullable = false)]
    public string SessionId { get; set; } = "";

    [SugarColumn(Length = 32, IsNullable = false)]
    public string Role { get; set; } = "";

    [SugarColumn(ColumnDataType = "TEXT", IsNullable = false)]
    public string Content { get; set; } = "";

    [SugarColumn(ColumnName = "extras_json", ColumnDataType = "TEXT", IsNullable = true)]
    public string? ExtrasJson { get; set; }

    [SugarColumn(ColumnName = "created_at")]
    public long CreatedAt { get; set; }
}
