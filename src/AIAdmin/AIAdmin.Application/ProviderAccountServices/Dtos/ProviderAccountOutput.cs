namespace AIAdmin.Application.ProviderAccountServices.Dtos;

public class ProviderAccountOutput
{
    public long Id { get; set; }
    public string Provider { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Endpoint { get; set; }
    public bool HasApiKey { get; set; }
    public string? ConfigJson { get; set; }
    public int SortOrder { get; set; }
    public bool Enabled { get; set; }
    public string? Remark { get; set; }
    public DateTime CreateTime { get; set; }
}
