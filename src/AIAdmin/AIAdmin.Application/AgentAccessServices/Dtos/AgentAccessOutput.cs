namespace AIAdmin.Application.AgentAccessServices.Dtos;

public class AgentAccessOutput
{
    public string AgentId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Provider { get; set; } = "";
    public bool Assigned { get; set; }
}

public class ResourceAccessOutput
{
    public long ResourceRowId { get; set; }
    public string AgentId { get; set; } = "";
    public string ResourceType { get; set; } = "";
    public string ExternalId { get; set; } = "";
    public string? DisplayName { get; set; }
    public bool Assigned { get; set; }
}
