using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using AIAdmin.Application.CozeDiscovery.Dtos;
using Volo.Abp.DependencyInjection;

namespace AIAdmin.Application.CozeDiscovery;

public sealed class CozeApiClient(IHttpClientFactory httpClientFactory) : ISingletonDependency
{
    private const int MaxPages = 20;
    private const int PageSize = 50;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<IReadOnlyList<CozeWorkspaceItem>> ListWorkspacesAsync(
        string endpoint,
        string apiKey,
        CancellationToken cancellationToken = default)
    {
        var host = NormalizeEndpoint(endpoint);
        var items = new List<CozeWorkspaceItem>();
        var page = 1;

        while (page <= MaxPages)
        {
            var url = $"https://{host}/v1/workspaces?page_num={page}&page_size={PageSize}";
            var envelope = await GetAsync<WorkspaceListData>(url, apiKey, cancellationToken);
            var pageItems = envelope?.Workspaces ?? [];
            foreach (var w in pageItems)
            {
                if (!w.Id.IsNullOrWhiteSpace())
                    items.Add(new CozeWorkspaceItem(w.Id.Trim(), w.Name ?? w.Id, w.IconUrl));
            }

            if (pageItems.Count < PageSize) break;
            page++;
        }

        return items;
    }

    public async Task<IReadOnlyList<CozeBotItem>> ListBotsAsync(
        string endpoint,
        string apiKey,
        string spaceId,
        CancellationToken cancellationToken = default)
    {
        var host = NormalizeEndpoint(endpoint);
        var items = new List<CozeBotItem>();
        var page = 1;

        while (page <= MaxPages)
        {
            var url =
                $"https://{host}/v1/space/published_bots_list?space_id={Uri.EscapeDataString(spaceId.Trim())}&page_index={page}&page_size={PageSize}";
            var envelope = await GetAsync<BotListData>(url, apiKey, cancellationToken);
            var pageItems = envelope?.SpaceBots ?? [];
            foreach (var b in pageItems)
            {
                if (!b.BotId.IsNullOrWhiteSpace())
                    items.Add(new CozeBotItem(
                        b.BotId.Trim(),
                        b.BotName ?? b.BotId,
                        b.Description,
                        b.IconUrl));
            }

            if (pageItems.Count < PageSize) break;
            page++;
        }

        return items;
    }

    public async Task<IReadOnlyList<CozeWorkflowItem>> ListWorkflowsAsync(
        string endpoint,
        string apiKey,
        string spaceId,
        string publishStatus,
        CancellationToken cancellationToken = default)
    {
        var host = NormalizeEndpoint(endpoint);
        var status = publishStatus.IsNullOrWhiteSpace() ? "published_online" : publishStatus.Trim();
        var items = new List<CozeWorkflowItem>();
        var page = 1;

        while (page <= MaxPages)
        {
            var url =
                $"https://{host}/v1/workflows?workspace_id={Uri.EscapeDataString(spaceId.Trim())}&page_num={page}&page_size={PageSize}&publish_status={Uri.EscapeDataString(status)}&workflow_mode=workflow";
            var envelope = await GetAsync<WorkflowListData>(url, apiKey, cancellationToken);
            var pageItems = envelope?.Items ?? [];
            foreach (var w in pageItems)
            {
                if (!w.WorkflowId.IsNullOrWhiteSpace())
                    items.Add(new CozeWorkflowItem(
                        w.WorkflowId.Trim(),
                        w.WorkflowName ?? w.WorkflowId,
                        w.Description,
                        w.IconUrl));
            }

            if (envelope?.HasMore != true) break;
            page++;
        }

        return items;
    }

    public async Task<IReadOnlyList<CozeWorkflowItem>> ListBotWorkflowsAsync(
        string endpoint,
        string apiKey,
        string botId,
        CancellationToken cancellationToken = default)
    {
        var host = NormalizeEndpoint(endpoint);
        var url = $"https://{host}/v1/bot/get_online_info?bot_id={Uri.EscapeDataString(botId.Trim())}";
        var data = await GetAsync<BotOnlineData>(url, apiKey, cancellationToken);
        return (data?.WorkflowInfo ?? [])
            .Where(w => !w.Id.IsNullOrWhiteSpace())
            .Select(w => new CozeWorkflowItem(w.Id!.Trim(), w.Name ?? w.Id!, w.Description, null))
            .ToList();
    }

    private async Task<TData?> GetAsync<TData>(string url, string apiKey, CancellationToken cancellationToken) where TData : class
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());

        using var client = httpClientFactory.CreateClient(nameof(CozeApiClient));
        using var response = await client.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw PersistdValidateException.Message($"Coze API 请求失败 ({(int)response.StatusCode}): {Truncate(json)}");

        var envelope = JsonSerializer.Deserialize<CozeEnvelope<TData>>(json, JsonOptions);
        if (envelope?.Code is not null and not 0)
            throw PersistdValidateException.Message(envelope.Msg ?? $"Coze API 错误码 {envelope.Code}");

        return envelope?.Data;
    }

    private static string NormalizeEndpoint(string endpoint)
    {
        var value = (endpoint.IsNullOrWhiteSpace() ? "api.coze.cn" : endpoint).Trim();
        if (value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            value = value["https://".Length..];
        if (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            value = value["http://".Length..];
        return value.TrimEnd('/');
    }

    private static string Truncate(string? text, int max = 240) =>
        string.IsNullOrEmpty(text) || text.Length <= max ? text ?? "" : text[..max] + "...";

    private sealed class CozeEnvelope<T>
    {
        public int? Code { get; set; }
        public string? Msg { get; set; }
        public T? Data { get; set; }
    }

    private sealed class WorkspaceListData
    {
        public List<WorkspaceRow>? Workspaces { get; set; }
    }

    private sealed class WorkspaceRow
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? IconUrl { get; set; }
    }

    private sealed class BotListData
    {
        [JsonPropertyName("space_bots")]
        public List<BotRow>? SpaceBots { get; set; }
    }

    private sealed class BotRow
    {
        public string? BotId { get; set; }
        public string? BotName { get; set; }
        public string? Description { get; set; }
        public string? IconUrl { get; set; }
    }

    private sealed class WorkflowListData
    {
        public List<WorkflowRow>? Items { get; set; }
        public bool? HasMore { get; set; }
    }

    private sealed class WorkflowRow
    {
        public string? WorkflowId { get; set; }
        public string? WorkflowName { get; set; }
        public string? Description { get; set; }
        public string? IconUrl { get; set; }
    }

    private sealed class BotOnlineData
    {
        public List<BotWorkflowRow>? WorkflowInfo { get; set; }
    }

    private sealed class BotWorkflowRow
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
    }
}
