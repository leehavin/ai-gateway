namespace DataChat.Core.Entities;

public sealed class ChatSession
{
    public required string Id { get; init; }
    public string Title { get; set; } = "新会话";
    public required string DomainId { get; set; }
    public required string ChatMode { get; set; }
    public string? Model { get; set; }
    public string? ResourceId { get; set; }
    public long CreatedAt { get; init; }
    public long UpdatedAt { get; set; }
}
