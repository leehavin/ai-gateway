namespace DataChat.Core.Entities;

public sealed class ChatMessage
{
    public required string Id { get; init; }
    public required string SessionId { get; init; }
    public required string Role { get; init; }
    public string Content { get; set; } = "";
    public string? ExtrasJson { get; set; }
    public long CreatedAt { get; init; }
}
