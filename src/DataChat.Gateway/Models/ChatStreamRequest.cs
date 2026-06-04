namespace DataChat.Gateway.Models;

public sealed class ChatStreamRequest
{
    public string? SessionId { get; set; }
    public required string Domain { get; set; }
    public required string Message { get; set; }
    public bool Stream { get; set; } = true;
    public List<ChatStreamMessage>? Messages { get; set; }
    public List<ChatAttachmentDto>? Attachments { get; set; }
    public ChatParametersDto? Parameters { get; set; }
}

public sealed class ChatStreamMessage
{
    public required string Role { get; set; }
    public required string Content { get; set; }
}
