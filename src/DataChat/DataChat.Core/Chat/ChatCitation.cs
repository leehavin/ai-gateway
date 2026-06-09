namespace DataChat.Core.Chat;

public sealed class ChatCitation
{
    public required string Title { get; init; }
    public string? Url { get; init; }
    public string? Snippet { get; init; }
}
