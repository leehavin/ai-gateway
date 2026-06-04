namespace DataChat.Gateway.Models;

public sealed class FeedbackRequest
{
    public required string SessionId { get; set; }
    public required string MessageId { get; set; }
    public required string Domain { get; set; }
    /// <summary>up 或 down</summary>
    public required string Rating { get; set; }
    public string? Comment { get; set; }
}
