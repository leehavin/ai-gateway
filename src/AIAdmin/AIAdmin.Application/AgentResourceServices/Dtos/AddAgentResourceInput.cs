using System.ComponentModel.DataAnnotations;

namespace AIAdmin.Application.AgentResourceServices.Dtos;

public class AddAgentResourceInput
{
    [Required]
    [MaxLength(64)]
    public string AgentId { get; set; } = "";

    [Required]
    [MaxLength(32)]
    public string ResourceType { get; set; } = "workflow";

    [Required]
    [MaxLength(128)]
    public string ExternalId { get; set; } = "";

    [MaxLength(256)]
    public string? DisplayName { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    public string? ConfigJson { get; set; }

    public int SortOrder { get; set; }

    public bool Enabled { get; set; } = true;

    [MaxLength(1000)]
    public string? Remark { get; set; }
}
