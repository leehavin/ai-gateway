namespace AIAdmin.Application.AgentServices.Dtos;

public class AgentOutput
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Provider { get; set; } = "";
    public string ChatMode { get; set; } = "";
    public long? ProviderAccountId { get; set; }
    public string? ProviderAccountName { get; set; }
    public string? Model { get; set; }
    public string ConfigJson { get; set; } = "{}";
    public string? Placeholder { get; set; }
    public string? QuickPromptsJson { get; set; }
    public string? SystemPrompt { get; set; }
    public int SortOrder { get; set; }
    public bool Enabled { get; set; }
    public string? Remark { get; set; }
    public DateTime CreateTime { get; set; }
}
