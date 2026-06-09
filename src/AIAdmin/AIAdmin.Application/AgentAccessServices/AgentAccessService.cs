using AIAdmin.Application.AgentAccessServices.Dtos;

namespace AIAdmin.Application.AgentAccessServices;

[ApiExplorerSettings(GroupName = ApiExplorerGroupConst.AGENT)]
public class AgentAccessService(ISqlSugarClient db, Repository<RoleEntity> roleRepository) : ApplicationService
{
    public async Task<List<AgentAccessOutput>> GetAgentsAsync(long roleId)
    {
        _ = await roleRepository.GetByIdAsync(roleId)
            ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult);

        var assigned = await db.Queryable<RoleAgentEntity>()
            .Where(x => x.RoleId == roleId)
            .Select(x => x.AgentId)
            .ToListAsync();
        var assignedSet = assigned.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var agents = await db.Queryable<AgentEntity>()
            .Where(x => x.Enabled)
            .OrderBy(x => x.SortOrder)
            .ToListAsync();

        return agents.Select(a => new AgentAccessOutput
        {
            AgentId = a.Id,
            DisplayName = a.DisplayName,
            Provider = a.Provider,
            Assigned = assignedSet.Contains(a.Id)
        }).ToList();
    }

    [UnitOfWork]
    public async Task AssignAgentsAsync(long roleId, string[] agentIds)
    {
        _ = await roleRepository.GetByIdAsync(roleId)
            ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult);

        _ = await db.Deleteable<RoleAgentEntity>().Where(x => x.RoleId == roleId).ExecuteCommandAsync();

        if (agentIds is { Length: > 0 })
        {
            var distinct = agentIds.Where(x => !x.IsNullOrWhiteSpace()).Select(x => x.Trim()).Distinct().ToArray();
            var rows = distinct.Select(id => new RoleAgentEntity
            {
                RoleId = roleId,
                AgentId = id,
                CreateTime = DateTime.Now
            }).ToList();
            _ = await db.Insertable(rows).ExecuteCommandAsync();
        }
    }

    public async Task<List<ResourceAccessOutput>> GetResourcesAsync(long roleId, string? agent = null, string? resourceType = "workflow")
    {
        _ = await roleRepository.GetByIdAsync(roleId)
            ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult);

        var assigned = await db.Queryable<RoleResourceEntity>()
            .Where(x => x.RoleId == roleId)
            .Select(x => x.ResourceRowId)
            .ToListAsync();
        var assignedSet = assigned.ToHashSet();

        var resources = await db.Queryable<AgentResourceEntity>()
            .Where(x => x.Enabled)
            .WhereIF(!agent.IsNullOrEmpty(), x => x.AgentId == agent!.Trim())
            .WhereIF(!resourceType.IsNullOrEmpty(), x => x.ResourceType == resourceType!.Trim())
            .OrderBy(x => x.AgentId)
            .OrderBy(x => x.SortOrder)
            .ToListAsync();

        return resources.Select(r => new ResourceAccessOutput
        {
            ResourceRowId = r.Id,
            AgentId = r.AgentId,
            ResourceType = r.ResourceType,
            ExternalId = r.ExternalId,
            DisplayName = r.DisplayName ?? r.ExternalId,
            Assigned = assignedSet.Contains(r.Id)
        }).ToList();
    }

    [UnitOfWork]
    public async Task AssignResourcesAsync(long roleId, long[] resourceRowIds)
    {
        _ = await roleRepository.GetByIdAsync(roleId)
            ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult);

        _ = await db.Deleteable<RoleResourceEntity>().Where(x => x.RoleId == roleId).ExecuteCommandAsync();

        if (resourceRowIds is { Length: > 0 })
        {
            var distinct = resourceRowIds.Distinct().ToArray();
            var rows = distinct.Select(id => new RoleResourceEntity
            {
                RoleId = roleId,
                ResourceRowId = id,
                CreateTime = DateTime.Now
            }).ToList();
            _ = await db.Insertable(rows).ExecuteCommandAsync();
        }
    }
}
