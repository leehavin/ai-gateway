namespace AIAdmin.Application.ProviderAccountServices.Dtos;

public class GetPagedListInput : PaginationParams
{
    public string? Name { get; set; }
    public string? Provider { get; set; }
}
