using AIAdmin.Application.CozeDiscovery.Dtos;

namespace AIAdmin.Application.CozeDiscovery;

[ApiExplorerSettings(GroupName = ApiExplorerGroupConst.AGENT)]
public class CozeDiscoveryService(
    Repository<ProviderAccountEntity> accountRepository,
    CozeApiClient cozeApi) : ApplicationService
{
    public async Task<List<CozeWorkspaceOutput>> GetWorkspacesAsync(long providerAccountId)
    {
        var (endpoint, apiKey) = await LoadCozeCredentialsAsync(providerAccountId);
        var list = await cozeApi.ListWorkspacesAsync(endpoint, apiKey);
        return list.Select(w => new CozeWorkspaceOutput
        {
            Id = w.Id,
            Name = w.Name,
            IconUrl = w.IconUrl
        }).ToList();
    }

    public async Task<List<CozeBotOutput>> GetBotsAsync(long providerAccountId, string space)
    {
        if (space.IsNullOrWhiteSpace())
            throw PersistdValidateException.Message("请选择扣子空间");

        var (endpoint, apiKey) = await LoadCozeCredentialsAsync(providerAccountId);
        var list = await cozeApi.ListBotsAsync(endpoint, apiKey, space);
        return list.Select(b => new CozeBotOutput
        {
            BotId = b.BotId,
            Name = b.Name,
            Description = b.Description,
            IconUrl = b.IconUrl,
            IsPublished = b.IsPublished
        }).ToList();
    }

    public async Task<List<CozeWorkflowOutput>> GetWorkflowsAsync(
        long providerAccountId,
        string space,
        string? bot = null,
        string? publishStatus = "published_online")
    {
        if (space.IsNullOrWhiteSpace())
            throw PersistdValidateException.Message("请选择扣子空间");

        var (endpoint, apiKey) = await LoadCozeCredentialsAsync(providerAccountId);
        var byId = new Dictionary<string, CozeWorkflowOutput>(StringComparer.OrdinalIgnoreCase);

        var workspaceFlows = await cozeApi.ListWorkflowsAsync(
            endpoint, apiKey, space, publishStatus ?? "published_online");
        foreach (var w in workspaceFlows)
        {
            byId[w.WorkflowId] = new CozeWorkflowOutput
            {
                WorkflowId = w.WorkflowId,
                Name = w.Name,
                Description = w.Description,
                IconUrl = w.IconUrl,
                Source = "workspace"
            };
        }

        if (!bot.IsNullOrWhiteSpace())
        {
            try
            {
                var botFlows = await cozeApi.ListBotWorkflowsAsync(endpoint, apiKey, bot!);
                foreach (var w in botFlows)
                {
                    if (byId.ContainsKey(w.WorkflowId)) continue;
                    byId[w.WorkflowId] = new CozeWorkflowOutput
                    {
                        WorkflowId = w.WorkflowId,
                        Name = w.Name,
                        Description = w.Description,
                        Source = "bot"
                    };
                }
            }
            catch
            {
                /* Bot 绑定工作流拉取失败时保留空间工作流列表 */
            }
        }

        return byId.Values.OrderBy(x => x.Name).ToList();
    }

    private async Task<(string Endpoint, string ApiKey)> LoadCozeCredentialsAsync(long providerAccountId)
    {
        var account = await accountRepository.GetByIdAsync(providerAccountId)
            ?? throw PersistdValidateException.Message("连接器不存在");

        if (!account.Enabled)
            throw PersistdValidateException.Message("连接器已禁用");

        if (!account.Provider.Equals("coze", StringComparison.OrdinalIgnoreCase))
            throw PersistdValidateException.Message("仅支持 Coze 连接器");

        if (account.ApiKeyCiphertext.IsNullOrWhiteSpace())
            throw PersistdValidateException.Message("请先在连接器中配置 API Key");

        var endpoint = account.Endpoint.IsNullOrWhiteSpace() ? "api.coze.cn" : account.Endpoint.Trim();
        return (endpoint, account.ApiKeyCiphertext.Trim());
    }
}
