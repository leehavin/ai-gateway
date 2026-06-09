namespace AIAdmin.Application.AgentServices.Dtos;

public class GetPagedListInput : PaginationParams
{
    public string? DisplayName { get; set; }
    public string? Provider { get; set; }
    public bool? Enabled { get; set; }
}
