namespace AIAdmin.Application.AgentResourceServices.Dtos;

public class GetPagedListInput : PaginationParams
{
    public string? AgentId { get; set; }
    public string? ResourceType { get; set; }
    public string? DisplayName { get; set; }
}
