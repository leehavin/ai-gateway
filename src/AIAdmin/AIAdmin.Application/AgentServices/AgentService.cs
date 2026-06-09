using AIAdmin.Application.AgentServices.Dtos;
using AIAdmin.Application.Gateway;

namespace AIAdmin.Application.AgentServices;

[ApiExplorerSettings(GroupName = ApiExplorerGroupConst.AGENT)]
public class AgentService(ISqlSugarClient db, IDataChatGatewayNotifier gatewayNotifier) : ApplicationService
{
    private static readonly HashSet<string> AllowedProviders =
        new(StringComparer.OrdinalIgnoreCase) { "coze", "dbgpt", "openai" };

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
