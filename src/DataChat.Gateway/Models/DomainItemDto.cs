namespace DataChat.Gateway.Models;

public sealed class DomainItemDto
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required string ChatMode { get; init; }
    public required string Provider { get; init; }
    public string? Model { get; init; }
    public string? Placeholder { get; init; }
    public IReadOnlyList<string> QuickPrompts { get; init; } = [];
    public DbgptDomainInfoDto? Dbgpt { get; init; }
    public CozeDomainInfoDto? Coze { get; init; }
}

public sealed class CozeDomainInfoDto
{
    public string? BotId { get; init; }
    public string? Endpoint { get; init; }
}

public sealed class DbgptDomainInfoDto
{
    public string? ChatMode { get; init; }
    public string? AppId { get; init; }
    public string? DatasourceId { get; init; }
    public string? KnowledgeSpaceName { get; init; }
}

public sealed class GatewayStatusDto
{
    public required string Status { get; init; }
    public required string Service { get; init; }
    public bool UseMock { get; init; }
    public bool DbgptReachable { get; init; }
    public string? DbgptBaseUrl { get; init; }
    public int DomainCount { get; init; }
}
