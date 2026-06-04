namespace DataChat.Gateway.Models;

public sealed class SessionSummaryDto
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string DomainId { get; init; }
    public long UpdatedAt { get; init; }
    public string? Preview { get; init; }
}

public sealed class SessionMessageDto
{
    public required string Id { get; init; }
    public required string Role { get; init; }
    public required string Content { get; init; }
    public string? ExtrasJson { get; init; }
    public long CreatedAt { get; init; }
}

public sealed class SessionDetailDto
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string DomainId { get; init; }
    public long UpdatedAt { get; init; }
    public List<SessionMessageDto> Messages { get; init; } = [];
}

public sealed class SyncSessionRequest
{
    public required string DomainId { get; set; }
    public string? Title { get; set; }
    public List<SessionMessageDto>? Messages { get; set; }
}

public sealed class CreateSessionRequest
{
    public required string DomainId { get; set; }
    public string? Title { get; set; }
}
