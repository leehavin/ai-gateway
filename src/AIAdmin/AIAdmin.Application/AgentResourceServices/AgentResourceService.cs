using AIAdmin.Application.AgentResourceServices.Dtos;
using AIAdmin.Application.Gateway;

namespace AIAdmin.Application.AgentResourceServices;

[ApiExplorerSettings(GroupName = ApiExplorerGroupConst.AGENT)]
public class AgentResourceService(
    ISqlSugarClient db,
    Repository<AgentResourceEntity> repository,
    IDataChatGatewayNotifier gatewayNotifier) : ApplicationService
{
    private static readonly HashSet<string> AllowedTypes =
        new(StringComparer.OrdinalIgnoreCase) { "workflow", "tool", "skill" };

    public async Task<PagedList<AgentResourceOutput>> GetPagedListAsync(GetPagedListInput input)
    {
        var paged = await db.Queryable<AgentResourceEntity>()
            .LeftJoin<AgentEntity>((r, a) => r.AgentId == a.Id)
            .WhereIF(!input.AgentId.IsNullOrEmpty(), (r, a) => r.AgentId == input.AgentId!.Trim())
            .WhereIF(!input.ResourceType.IsNullOrEmpty(), (r, a) => r.ResourceType == input.ResourceType!.Trim())
            .WhereIF(!input.DisplayName.IsNullOrEmpty(), (r, a) =>
                (r.DisplayName != null && r.DisplayName.Contains(input.DisplayName!.Trim())) ||
                r.ExternalId.Contains(input.DisplayName!.Trim()))
            .OrderBy((r, a) => r.SortOrder)
            .OrderByDescending((r, a) => r.CreateTime)
            .Select((r, a) => new AgentResourceEntity
            {
                Id = r.Id,
                AgentId = r.AgentId,
                ResourceType = r.ResourceType,
                ExternalId = r.ExternalId,
                DisplayName = r.DisplayName,
                Description = r.Description,
                ConfigJson = r.ConfigJson,
                SortOrder = r.SortOrder,
                Enabled = r.Enabled,
                Remark = r.Remark,
                CreateTime = r.CreateTime
            })
            .ToPurestPagedListAsync(input.PageIndex, input.PageSize);

        var agentNames = await LoadAgentNames(paged.Items.Select(x => x.AgentId).Distinct());
        var result = paged.Adapt<PagedList<AgentResourceOutput>>();
        result.Items = paged.Items.Select(r => Map(r, agentNames.GetValueOrDefault(r.AgentId))).ToList();
        return result;
    }

    public async Task<List<AgentResourceOutput>> GetByAgentAsync(string agentId, string? resourceType = null)
    {
        var list = await db.Queryable<AgentResourceEntity>()
            .Where(x => x.AgentId == agentId)
            .WhereIF(!resourceType.IsNullOrEmpty(), x => x.ResourceType == resourceType!.Trim())
            .OrderBy(x => x.SortOrder)
            .ToListAsync();
        var agent = await db.Queryable<AgentEntity>().FirstAsync(x => x.Id == agentId);
        return list.Select(r => Map(r, agent?.DisplayName)).ToList();
    }

    public async Task<AgentResourceOutput> GetAsync(long id)
    {
        var entity = await repository.GetByIdAsync(id)
            ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult);
        var agent = await db.Queryable<AgentEntity>().FirstAsync(x => x.Id == entity.AgentId);
        return Map(entity, agent?.DisplayName);
    }

    public async Task<long> AddAsync(AddAgentResourceInput input)
    {
        await EnsureAgentExists(input.AgentId);
        ValidateResource(input.ResourceType, input.ExternalId);
        if (await db.Queryable<AgentResourceEntity>().AnyAsync(x =>
                x.AgentId == input.AgentId.Trim() &&
                x.ResourceType == input.ResourceType.Trim() &&
                x.ExternalId == input.ExternalId.Trim()))
            throw PersistdValidateException.Message("同智能体下该资源已存在");

        var entity = input.Adapt<AgentResourceEntity>();
        entity.AgentId = input.AgentId.Trim();
        entity.ResourceType = input.ResourceType.Trim();
        entity.ExternalId = input.ExternalId.Trim();
        var id = await repository.InsertReturnSnowflakeIdAsync(entity);
        gatewayNotifier.NotifyReloadDomains();
        return id;
    }

    public async Task PutAsync(long id, AddAgentResourceInput input)
    {
        await EnsureAgentExists(input.AgentId);
        ValidateResource(input.ResourceType, input.ExternalId);
        var entity = await repository.GetByIdAsync(id)
            ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult);

        var updated = input.Adapt(entity);
        updated.Id = id;
        _ = await repository.UpdateAsync(updated);
        gatewayNotifier.NotifyReloadDomains();
    }

    public async Task DeleteAsync(long id)
    {
        var entity = await repository.GetByIdAsync(id)
            ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult);
        _ = await db.Deleteable<RoleResourceEntity>().Where(x => x.ResourceRowId == id).ExecuteCommandAsync();
        await repository.DeleteAsync(entity);
        gatewayNotifier.NotifyReloadDomains();
    }

    private async Task EnsureAgentExists(string agentId)
    {
        if (!await db.Queryable<AgentEntity>().AnyAsync(x => x.Id == agentId.Trim()))
            throw PersistdValidateException.Message("智能体不存在");
    }

    private static void ValidateResource(string resourceType, string externalId)
    {
        if (!AllowedTypes.Contains(resourceType))
            throw PersistdValidateException.Message("不支持的 resourceType");
        if (externalId.IsNullOrWhiteSpace())
            throw PersistdValidateException.Message("externalId 不能为空");
    }

    private async Task<Dictionary<string, string>> LoadAgentNames(IEnumerable<string> agentIds)
    {
        var ids = agentIds.ToList();
        if (ids.Count == 0) return [];
        var rows = await db.Queryable<AgentEntity>()
            .Where(x => ids.Contains(x.Id))
            .Select(x => new { x.Id, x.DisplayName })
            .ToListAsync();
        return rows.ToDictionary(x => x.Id, x => x.DisplayName);
    }

    private static AgentResourceOutput Map(AgentResourceEntity e, string? agentDisplayName) => new()
    {
        Id = e.Id,
        AgentId = e.AgentId,
        AgentDisplayName = agentDisplayName,
        ResourceType = e.ResourceType,
        ExternalId = e.ExternalId,
        DisplayName = e.DisplayName,
        Description = e.Description,
        ConfigJson = e.ConfigJson,
        SortOrder = e.SortOrder,
        Enabled = e.Enabled,
        Remark = e.Remark,
        CreateTime = e.CreateTime
    };
}
