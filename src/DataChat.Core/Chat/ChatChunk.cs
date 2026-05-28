namespace DataChat.Core.Chat;

public sealed class ChatChunk
{
    public string? TextDelta { get; init; }
    public bool IsCompleted { get; init; }
    public string? Error { get; init; }
}
