namespace AIAdmin.Application.AgentResourceServices.Dtos;

public class AgentResourceOutput
{
    public long Id { get; set; }
    public string AgentId { get; set; } = "";
    public string? AgentDisplayName { get; set; }
    public string ResourceType { get; set; } = "";
    public string ExternalId { get; set; } = "";
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string? ConfigJson { get; set; }
    public int SortOrder { get; set; }
    public bool Enabled { get; set; }
    public string? Remark { get; set; }
    public DateTime CreateTime { get; set; }
}
