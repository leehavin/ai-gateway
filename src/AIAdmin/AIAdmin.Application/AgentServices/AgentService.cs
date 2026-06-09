using AIAdmin.Application.AgentResourceServices.Dtos;
using AIAdmin.Application.AgentServices.Dtos;
using AIAdmin.Application.CozeDiscovery;
using AIAdmin.Application.Gateway;
using System.Text.Json;

namespace AIAdmin.Application.AgentServices;

[ApiExplorerSettings(GroupName = ApiExplorerGroupConst.AGENT)]
public class AgentService(
    ISqlSugarClient db,
    Repository<AgentResourceEntity> resourceRepository,
    IDataChatGatewayNotifier gatewayNotifier,
    CozeDiscoveryService cozeDiscovery) : ApplicationService
{
    private static readonly HashSet<string> AllowedProviders =
        new(StringComparer.OrdinalIgnoreCase) { "coze", "dbgpt", "openai" };

    private static readonly HashSet<string> AllowedResourceTypes =
        new(StringComparer.OrdinalIgnoreCase) { "workflow", "tool", "skill" };

    public async Task<PagedList<AgentOutput>> GetPagedListAsync(GetPagedListInput input)
    {
        var paged = await db.Queryable<AgentEntity>()
            .LeftJoin<ProviderAccountEntity>((a, p) => a.ProviderAccountId == p.Id)
            .WhereIF(!input.DisplayName.IsNullOrEmpty(), (a, p) => a.DisplayName.Contains(input.DisplayName!.Trim()))
            .WhereIF(!input.Provider.IsNullOrEmpty(), (a, p) => a.Provider == input.Provider!.Trim())
            .WhereIF(input.Enabled.HasValue, (a, p) => a.Enabled == input.Enabled!.Value)
            .OrderBy((a, p) => a.SortOrder)
            .OrderByDescending((a, p) => a.CreateTime)
            .Select((a, p) => new AgentEntity
            {
                Id = a.Id,
                DisplayName = a.DisplayName,
                Provider = a.Provider,
                ChatMode = a.ChatMode,
                ProviderAccountId = a.ProviderAccountId,
                ProviderAccountName = p.Name,
                Model = a.Model,
                ConfigJson = a.ConfigJson,
                Placeholder = a.Placeholder,
                QuickPromptsJson = a.QuickPromptsJson,
                SystemPrompt = a.SystemPrompt,
                SortOrder = a.SortOrder,
                Enabled = a.Enabled,
                Remark = a.Remark,
                CreateTime = a.CreateTime
            })
            .ToPurestPagedListAsync(input.PageIndex, input.PageSize);
        return paged.Adapt<PagedList<AgentOutput>>();
    }

    public async Task<List<AgentOutput>> GetAgentsAsync(string? provider, bool? enabled = true)
    {
        var list = await db.Queryable<AgentEntity>()
            .LeftJoin<ProviderAccountEntity>((a, p) => a.ProviderAccountId == p.Id)
            .WhereIF(!provider.IsNullOrEmpty(), (a, p) => a.Provider == provider!.Trim())
            .WhereIF(enabled.HasValue, (a, p) => a.Enabled == enabled!.Value)
            .OrderBy((a, p) => a.SortOrder)
            .Select((a, p) => new AgentEntity
            {
                Id = a.Id,
                DisplayName = a.DisplayName,
                Provider = a.Provider,
                ChatMode = a.ChatMode,
                ProviderAccountId = a.ProviderAccountId,
                ProviderAccountName = p.Name,
                Model = a.Model,
                ConfigJson = a.ConfigJson,
                Enabled = a.Enabled,
                SortOrder = a.SortOrder
            })
            .ToListAsync();
        return list.Adapt<List<AgentOutput>>();
    }

    public async Task<AgentOutput> GetAsync(string id)
    {
        var entity = await db.Queryable<AgentEntity>()
            .LeftJoin<ProviderAccountEntity>((a, p) => a.ProviderAccountId == p.Id)
            .Where((a, p) => a.Id == id)
            .Select((a, p) => new AgentEntity
            {
                Id = a.Id,
                DisplayName = a.DisplayName,
                Provider = a.Provider,
                ChatMode = a.ChatMode,
                ProviderAccountId = a.ProviderAccountId,
                ProviderAccountName = p.Name,
                Model = a.Model,
                ConfigJson = a.ConfigJson,
                Placeholder = a.Placeholder,
                QuickPromptsJson = a.QuickPromptsJson,
                SystemPrompt = a.SystemPrompt,
                SortOrder = a.SortOrder,
                Enabled = a.Enabled,
                Remark = a.Remark,
                CreateTime = a.CreateTime
            })
            .FirstAsync();
        return entity.Adapt<AgentOutput>();
    }

    public async Task<string> AddAsync(AddAgentInput input)
    {
        ValidateAgent(input.Id, input.Provider, input.ConfigJson);
        if (await db.Queryable<AgentEntity>().AnyAsync(x => x.Id == input.Id.Trim()))
            throw PersistdValidateException.Message("智能体 ID 已存在");
        if (input.ProviderAccountId.HasValue &&
            !await db.Queryable<ProviderAccountEntity>().AnyAsync(x => x.Id == input.ProviderAccountId.Value))
            throw PersistdValidateException.Message("连接器不存在");

        var entity = input.Adapt<AgentEntity>();
        entity.Id = input.Id.Trim();
        entity.CreateTime = DateTime.Now;
        _ = await db.Insertable(entity).ExecuteCommandAsync();
        gatewayNotifier.NotifyReloadDomains();
        return entity.Id;
    }

    public async Task PutAsync(string id, AddAgentInput input)
    {
        ValidateAgent(input.Id, input.Provider, input.ConfigJson);
        var entity = await db.Queryable<AgentEntity>().FirstAsync(x => x.Id == id)
            ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult);
        if (!string.Equals(id, input.Id.Trim(), StringComparison.OrdinalIgnoreCase) &&
            await db.Queryable<AgentEntity>().AnyAsync(x => x.Id == input.Id.Trim()))
            throw PersistdValidateException.Message("智能体 ID 已存在");

        var updated = input.Adapt(entity);
        updated.Id = id;
        updated.UpdateTime = DateTime.Now;
        _ = await db.Updateable(updated).ExecuteCommandAsync();
        gatewayNotifier.NotifyReloadDomains();
    }

    public async Task DeleteAsync(string id)
    {
        var entity = await db.Queryable<AgentEntity>().FirstAsync(x => x.Id == id)
            ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult);
        _ = await db.Deleteable<AgentResourceEntity>().Where(x => x.AgentId == id).ExecuteCommandAsync();
        _ = await db.Deleteable<RoleAgentEntity>().Where(x => x.AgentId == id).ExecuteCommandAsync();
        _ = await db.Deleteable(entity).ExecuteCommandAsync();
        gatewayNotifier.NotifyReloadDomains();
    }

    public async Task<List<AgentResourceOutput>> GetResourcesAsync(string agentId, string? resourceType = null)
    {
        await EnsureAgentExists(agentId);
        var list = await db.Queryable<AgentResourceEntity>()
            .Where(x => x.AgentId == agentId)
            .WhereIF(!resourceType.IsNullOrEmpty(), x => x.ResourceType == resourceType!.Trim())
            .OrderBy(x => x.SortOrder)
            .ToListAsync();
        var agent = await db.Queryable<AgentEntity>().FirstAsync(x => x.Id == agentId);
        return list.Select(r => MapResource(r, agent?.DisplayName)).ToList();
    }

    public async Task<long> AddResourceAsync(AddAgentResourceInput input)
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
        var id = await resourceRepository.InsertReturnSnowflakeIdAsync(entity);
        gatewayNotifier.NotifyReloadDomains();
        return id;
    }

    public async Task PutResourceAsync(long id, AddAgentResourceInput input)
    {
        await EnsureAgentExists(input.AgentId);
        ValidateResource(input.ResourceType, input.ExternalId);
        var entity = await resourceRepository.GetByIdAsync(id)
            ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult);

        var updated = input.Adapt(entity);
        updated.Id = id;
        _ = await resourceRepository.UpdateAsync(updated);
        gatewayNotifier.NotifyReloadDomains();
    }

    public async Task DeleteResourceAsync(long id)
    {
        var entity = await resourceRepository.GetByIdAsync(id)
            ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult);
        _ = await db.Deleteable<RoleResourceEntity>().Where(x => x.ResourceRowId == id).ExecuteCommandAsync();
        await resourceRepository.DeleteAsync(entity);
        gatewayNotifier.NotifyReloadDomains();
    }

    public async Task<int> SyncCozeWorkflowsAsync(string agentId)
    {
        var agent = await db.Queryable<AgentEntity>().FirstAsync(x => x.Id == agentId)
            ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult);

        if (!agent.Provider.Equals("coze", StringComparison.OrdinalIgnoreCase))
            throw PersistdValidateException.Message("仅 Coze 智能体支持同步工作流");

        if (!agent.ProviderAccountId.HasValue)
            throw PersistdValidateException.Message("请先绑定连接器");

        var cfg = ParseCozeConfig(agent.ConfigJson);
        if (cfg.WorkspaceId.IsNullOrWhiteSpace())
            throw PersistdValidateException.Message("请先配置扣子空间");

        var remote = await cozeDiscovery.GetWorkflowsAsync(
            agent.ProviderAccountId.Value,
            cfg.WorkspaceId,
            cfg.BotId,
            cfg.ListPublishStatus);

        var existing = await db.Queryable<AgentResourceEntity>()
            .Where(x => x.AgentId == agentId && x.ResourceType == "workflow")
            .Select(x => x.ExternalId)
            .ToListAsync();
        var existingSet = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);

        var added = 0;
        var sort = (await db.Queryable<AgentResourceEntity>()
            .Where(x => x.AgentId == agentId)
            .MaxAsync(x => (int?)x.SortOrder)) ?? 0;

        foreach (var wf in remote)
        {
            if (existingSet.Contains(wf.WorkflowId)) continue;

            sort++;
            _ = await resourceRepository.InsertReturnSnowflakeIdAsync(new AgentResourceEntity
            {
                AgentId = agentId,
                ResourceType = "workflow",
                ExternalId = wf.WorkflowId,
                DisplayName = wf.Name,
                Description = wf.Description,
                ConfigJson = """{"inputParameter":"BOT_USER_INPUT","inputHint":""}""",
                SortOrder = sort,
                Enabled = true
            });
            existingSet.Add(wf.WorkflowId);
            added++;
        }

        if (added > 0)
            gatewayNotifier.NotifyReloadDomains();

        return added;
    }

    private async Task EnsureAgentExists(string agentId)
    {
        if (!await db.Queryable<AgentEntity>().AnyAsync(x => x.Id == agentId.Trim()))
            throw PersistdValidateException.Message("智能体不存在");
    }

    private static void ValidateResource(string resourceType, string externalId)
    {
        if (!AllowedResourceTypes.Contains(resourceType))
            throw PersistdValidateException.Message("不支持的 resourceType");
        if (externalId.IsNullOrWhiteSpace())
            throw PersistdValidateException.Message("externalId 不能为空");
    }

    private static AgentResourceOutput MapResource(AgentResourceEntity e, string? agentDisplayName) => new()
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

    private static CozeAgentConfig ParseCozeConfig(string? configJson)
    {
        if (configJson.IsNullOrWhiteSpace()) return new CozeAgentConfig();
        try
        {
            return JsonSerializer.Deserialize<CozeAgentConfig>(configJson) ?? new CozeAgentConfig();
        }
        catch
        {
            return new CozeAgentConfig();
        }
    }

    private sealed class CozeAgentConfig
    {
        public string? BotId { get; set; }
        public string? WorkspaceId { get; set; }
        public string? ListPublishStatus { get; set; }
    }

    private static void ValidateAgent(string id, string provider, string configJson)
    {
        if (id.IsNullOrWhiteSpace())
            throw PersistdValidateException.Message("智能体 ID 不能为空");
        if (!AllowedProviders.Contains(provider))
            throw PersistdValidateException.Message("不支持的 provider");
        if (configJson.IsNullOrWhiteSpace())
            throw PersistdValidateException.Message("configJson 不能为空");
    }
}
