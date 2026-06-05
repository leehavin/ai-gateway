namespace DataChat.Core.Chat;

public sealed class ChatChunk
{
    public string? TextDelta { get; init; }
    public string? ThinkingDelta { get; init; }
    public IReadOnlyList<ChatCitation>? Citations { get; init; }
    public bool IsCompleted { get; init; }
    public string? Error { get; init; }
    public CozeWorkflowInterruptInfo? WorkflowInterrupt { get; init; }
    public string? WorkflowDebugUrl { get; init; }
}

public sealed class CozeWorkflowInterruptInfo
{
    public required string WorkflowId { get; init; }
    public required string EventId { get; init; }
    public required int InterruptType { get; init; }
    public string? NodeTitle { get; init; }
    public string? Prompt { get; init; }
}
