namespace AIAdmin.Application.CozeDiscovery.Dtos;

public sealed record CozeWorkspaceItem(string Id, string Name, string? IconUrl);

public sealed record CozeBotItem(string BotId, string Name, string? Description, string? IconUrl, bool IsPublished = true);

public sealed record CozeWorkflowItem(string WorkflowId, string Name, string? Description, string? IconUrl);

public sealed class CozeWorkspaceOutput
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string? IconUrl { get; set; }
}

public sealed class CozeBotOutput
{
    public string BotId { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? IconUrl { get; set; }
    public bool IsPublished { get; set; } = true;
}

public sealed class CozeWorkflowOutput
{
    public string WorkflowId { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? IconUrl { get; set; }
    public string Source { get; set; } = "workspace";
}
