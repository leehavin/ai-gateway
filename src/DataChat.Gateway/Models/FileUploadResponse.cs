namespace DataChat.Gateway.Models;

public sealed class FileUploadResponse
{
    public required string FileId { get; init; }
    public required string Name { get; init; }
    public required string ContentType { get; init; }
    public long Size { get; init; }
    public required string Url { get; init; }
    public string? TextPreview { get; init; }
}
