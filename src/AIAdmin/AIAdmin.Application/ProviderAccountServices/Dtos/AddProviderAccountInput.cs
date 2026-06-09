using System.ComponentModel.DataAnnotations;

namespace AIAdmin.Application.ProviderAccountServices.Dtos;

public class AddProviderAccountInput
{
    [Required]
    [MaxLength(32)]
    public string Provider { get; set; } = "";

    [Required]
    [MaxLength(128)]
    public string Name { get; set; } = "";

    [MaxLength(512)]
    public string? Endpoint { get; set; }

    public string? ApiKeyCiphertext { get; set; }

    public string? ConfigJson { get; set; }

    public int SortOrder { get; set; }

    public bool Enabled { get; set; } = true;

    [MaxLength(1000)]
    public string? Remark { get; set; }
}
