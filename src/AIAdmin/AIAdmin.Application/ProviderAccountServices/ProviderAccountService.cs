using AIAdmin.Application.Gateway;
using AIAdmin.Application.ProviderAccountServices.Dtos;

namespace AIAdmin.Application.ProviderAccountServices;

[ApiExplorerSettings(GroupName = ApiExplorerGroupConst.AGENT)]
public class ProviderAccountService(
    ISqlSugarClient db,
    Repository<ProviderAccountEntity> repository,
    IDataChatGatewayNotifier gatewayNotifier) : ApplicationService
{
    public async Task<PagedList<ProviderAccountOutput>> GetPagedListAsync(GetPagedListInput input)
    {
        var paged = await db.Queryable<ProviderAccountEntity>()
            .WhereIF(!input.Name.IsNullOrEmpty(), x => x.Name.Contains(input.Name!.Trim()))
            .WhereIF(!input.Provider.IsNullOrEmpty(), x => x.Provider == input.Provider!.Trim())
            .OrderBy(x => x.SortOrder)
            .OrderByDescending(x => x.CreateTime)
            .ToPurestPagedListAsync(input.PageIndex, input.PageSize);
        return MapPaged(paged);
    }

    public async Task<List<ProviderAccountOutput>> GetAccountsAsync(string? provider)
    {
        var list = await db.Queryable<ProviderAccountEntity>()
            .WhereIF(!provider.IsNullOrEmpty(), x => x.Provider == provider!.Trim())
            .Where(x => x.Enabled)
            .OrderBy(x => x.SortOrder)
            .ToListAsync();
        return list.Select(Map).ToList();
    }

    public async Task<ProviderAccountOutput> GetAsync(long id)
    {
        var entity = await repository.GetByIdAsync(id)
            ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult);
        return Map(entity);
    }

    public async Task<long> AddAsync(AddProviderAccountInput input)
    {
        ValidateKey(input.Provider, input.ApiKeyCiphertext);
        var entity = input.Adapt<ProviderAccountEntity>();
        var id = await repository.InsertReturnSnowflakeIdAsync(entity);
        gatewayNotifier.NotifyReloadDomains();
        return id;
    }

    public async Task PutAsync(long id, AddProviderAccountInput input)
    {
        var entity = await repository.GetByIdAsync(id)
            ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult);
        if (input.ApiKeyCiphertext.IsNullOrEmpty())
            input.ApiKeyCiphertext = entity.ApiKeyCiphertext;
        if (input.ConfigJson.IsNullOrEmpty())
            input.ConfigJson = entity.ConfigJson;
        ValidateKey(input.Provider, input.ApiKeyCiphertext);
        var updated = input.Adapt(entity);
        _ = await repository.UpdateAsync(updated);
        gatewayNotifier.NotifyReloadDomains();
    }

    public async Task DeleteAsync(long id)
    {
        var entity = await repository.GetByIdAsync(id)
            ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult);
        var inUse = await db.Queryable<AgentEntity>().AnyAsync(x => x.ProviderAccountId == id);
        if (inUse)
            throw PersistdValidateException.Message("连接器已被智能体引用，无法删除");
        await repository.DeleteAsync(entity);
        gatewayNotifier.NotifyReloadDomains();
    }

    private static void ValidateKey(string provider, string? ciphertext)
    {
        if (provider.Equals("dbgpt", StringComparison.OrdinalIgnoreCase))
            return;
        if (ciphertext.IsNullOrEmpty())
            throw PersistdValidateException.Message("请填写 API Key");
    }

    private static ProviderAccountOutput Map(ProviderAccountEntity e) => new()
    {
        Id = e.Id,
        Provider = e.Provider,
        Name = e.Name,
        Endpoint = e.Endpoint,
        HasApiKey = !e.ApiKeyCiphertext.IsNullOrEmpty(),
        ConfigJson = e.ConfigJson,
        SortOrder = e.SortOrder,
        Enabled = e.Enabled,
        Remark = e.Remark,
        CreateTime = e.CreateTime
    };

    private static PagedList<ProviderAccountOutput> MapPaged(PagedList<ProviderAccountEntity> paged)
    {
        var result = paged.Adapt<PagedList<ProviderAccountOutput>>();
        result.Items = paged.Items.Select(Map).ToList();
        return result;
    }
}
