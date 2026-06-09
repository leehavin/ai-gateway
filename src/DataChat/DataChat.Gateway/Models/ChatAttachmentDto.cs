namespace DataChat.Gateway.Models;

public sealed class ChatAttachmentDto
{
    public required string FileId { get; set; }
    public string? Name { get; set; }
}
