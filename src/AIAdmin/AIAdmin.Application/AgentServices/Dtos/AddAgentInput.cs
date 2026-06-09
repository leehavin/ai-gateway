using System.ComponentModel.DataAnnotations;

namespace AIAdmin.Application.AgentServices.Dtos;

public class AddAgentInput
{
    [Required]
    [MaxLength(64)]
    public string Id { get; set; } = "";

    [Required]
    [MaxLength(128)]
    public string DisplayName { get; set; } = "";

    [Required]
    [MaxLength(32)]
    public string Provider { get; set; } = "";

    [Required]
    [MaxLength(64)]
    public string ChatMode { get; set; } = "";

    public long? ProviderAccountId { get; set; }

    [MaxLength(128)]
    public string? Model { get; set; }

    public string ConfigJson { get; set; } = "{}";

    [MaxLength(512)]
    public string? Placeholder { get; set; }

    public string? QuickPromptsJson { get; set; }

    public string? SystemPrompt { get; set; }

    public int SortOrder { get; set; }

    public bool Enabled { get; set; } = true;

    [MaxLength(1000)]
    public string? Remark { get; set; }
}
