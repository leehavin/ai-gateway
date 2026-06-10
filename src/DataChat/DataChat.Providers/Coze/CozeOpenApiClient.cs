using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DataChat.Core.Abstractions;
using DataChat.Core.Chat;
using DataChat.Core.Configuration;

namespace DataChat.Providers.Coze;

/// <summary>Coze Open API（工作流列表 / stream_run / stream_resume、Bot get_online_info）。</summary>
public sealed class CozeOpenApiClient
{
    private const int WorkflowListPageSize = 50;
    private const int WorkflowListMaxPages = 20;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly CozeClientFactory _clientFactory;
    private readonly IHttpClientFactory _httpClientFactory;

    public CozeOpenApiClient(CozeClientFactory clientFactory, IHttpClientFactory httpClientFactory)
    {
        _clientFactory = clientFactory;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IReadOnlyList<CozeWorkflowCatalogItem>> ListBotWorkflowsAsync(
        DomainProfile domain,
        GlobalDefaults defaults,
        CancellationToken cancellationToken = default)
    {
        var coze = domain.Coze ?? throw new InvalidOperationException("领域未配置 Coze。");
        var byId = BuildConfiguredWorkflowMap(coze);

        if (!string.IsNullOrWhiteSpace(coze.WorkspaceId))
        {
            try
            {
                var apiList = await ListWorkflowsFromApiAsync(coze, defaults, cancellationToken);
                MergeWorkflowCatalog(byId, apiList);
            }
            catch
            {
                /* workspace 列表 API 失败时继续尝试 Bot 绑定与本地配置 */
            }
        }

        // workspace 列表为空或未配置 workspaceId 时，回退 Bot 在线绑定的工作流
        if (byId.Count == 0 && !string.IsNullOrWhiteSpace(coze.BotId))
        {
            try
            {
                var meta = await GetBotOnlineInfoAsync(coze, defaults, cancellationToken);
                MergeWorkflowCatalog(byId, meta.Workflows.Select(w => new CozeWorkflowCatalogItem
                {
                    WorkflowId = w.Id,
                    DisplayName = w.Name ?? w.Id,
                    Description = w.Description,
                    InputParameter = "BOT_USER_INPUT"
                }));
            }
            catch
            {
                /* Bot 元数据失败时保留本地 workflows 或返回空列表 */
            }
        }

        return byId.Values.OrderBy(w => w.DisplayName).ToList();
    }

    private static Dictionary<string, CozeWorkflowCatalogItem> BuildConfiguredWorkflowMap(CozeDomainOptions coze)
    {
        var map = new Dictionary<string, CozeWorkflowCatalogItem>(StringComparer.OrdinalIgnoreCase);
        foreach (var w in coze.Workflows ?? [])
        {
            map[w.WorkflowId] = new CozeWorkflowCatalogItem
            {
                WorkflowId = w.WorkflowId,
                DisplayName = w.DisplayName ?? w.WorkflowId,
                Description = w.Description,
                InputParameter = string.IsNullOrWhiteSpace(w.InputParameter) ? "BOT_USER_INPUT" : w.InputParameter.Trim(),
                AppId = w.AppId
            };
        }
        return map;
    }

    private static void MergeWorkflowCatalog(
        Dictionary<string, CozeWorkflowCatalogItem> byId,
        IEnumerable<CozeWorkflowCatalogItem> incoming)
    {
        foreach (var wf in incoming)
        {
            if (byId.TryGetValue(wf.WorkflowId, out var existing))
            {
                byId[wf.WorkflowId] = new CozeWorkflowCatalogItem
                {
                    WorkflowId = wf.WorkflowId,
                    DisplayName = existing.DisplayName,
                    Description = existing.Description ?? wf.Description,
                    InputParameter = existing.InputParameter,
                    IconUrl = wf.IconUrl ?? existing.IconUrl,
                    AppId = existing.AppId ?? wf.AppId
                };
            }
            else
            {
                byId[wf.WorkflowId] = wf;
            }
        }
    }

    /// <summary>官方 GET /v1/workflows（<see href="https://docs.coze.cn/developer_guides/get_workflow_list"/>）。</summary>
    private async Task<List<CozeWorkflowCatalogItem>> ListWorkflowsFromApiAsync(
        CozeDomainOptions coze,
        GlobalDefaults defaults,
        CancellationToken cancellationToken)
    {
        var ctx = await _clientFactory.CreateContextAsync(coze, defaults, cancellationToken);

        var client = _httpClientFactory.CreateClient(CozeHttpClientNames.Client);
        var items = new List<CozeWorkflowCatalogItem>();
        var pageNum = 1;
        var publishStatus = string.IsNullOrWhiteSpace(coze.ListPublishStatus) ? "published_online" : coze.ListPublishStatus.Trim();

        while (pageNum <= WorkflowListMaxPages)
        {
            var query = new List<string>
            {
                $"workspace_id={Uri.EscapeDataString(coze.WorkspaceId!.Trim())}",
                $"page_num={pageNum}",
                $"page_size={WorkflowListPageSize}",
                $"publish_status={Uri.EscapeDataString(publishStatus)}",
                "workflow_mode=workflow"
            };
            if (!string.IsNullOrWhiteSpace(coze.ListAppId))
                query.Add($"app_id={Uri.EscapeDataString(coze.ListAppId.Trim())}");

            var url = $"https://{ctx.EndPoint}/v1/workflows?{string.Join('&', query)}";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ctx.AccessToken);

            using var response = await client.SendAsync(request, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Coze 工作流列表失败 ({(int)response.StatusCode}): {Truncate(json, 300)}");

            var envelope = JsonSerializer.Deserialize<CozeApiEnvelope<WorkflowListData>>(json, JsonOptions);
            if (envelope?.Code is not null and not 0)
                throw new InvalidOperationException(envelope.Msg ?? $"Coze 工作流列表错误码 {envelope.Code}");

            var pageItems = envelope?.Data?.Items ?? [];
            foreach (var row in pageItems)
            {
                if (string.IsNullOrWhiteSpace(row.WorkflowId)) continue;
                items.Add(new CozeWorkflowCatalogItem
                {
                    WorkflowId = row.WorkflowId,
                    DisplayName = string.IsNullOrWhiteSpace(row.WorkflowName) ? row.WorkflowId : row.WorkflowName,
                    Description = row.Description,
                    IconUrl = row.IconUrl,
                    AppId = string.IsNullOrWhiteSpace(row.AppId) ? null : row.AppId,
                    InputParameter = "BOT_USER_INPUT"
                });
            }

            if (envelope?.Data?.HasMore != true) break;
            pageNum++;
        }

        return items;
    }

    public async Task<CozeWorkflowCatalogItem?> ResolveWorkflowAsync(
        DomainProfile domain,
        GlobalDefaults defaults,
        string workflowId,
        CancellationToken cancellationToken = default)
    {
        var list = await ListBotWorkflowsAsync(domain, defaults, cancellationToken);
        return list.FirstOrDefault(w => string.Equals(w.WorkflowId, workflowId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>拉取工作流开始节点 input.parameters（版本列表 / 详情多路径尝试）。</summary>
    public async Task<IReadOnlyList<CozeWorkflowInputSpec>> TryGetWorkflowInputSpecsAsync(
        CozeDomainOptions coze,
        GlobalDefaults defaults,
        string workflowId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var ctx = await _clientFactory.CreateContextAsync(coze, defaults, cancellationToken);

            var client = _httpClientFactory.CreateClient(CozeHttpClientNames.Client);
            var id = Uri.EscapeDataString(workflowId.Trim());
            var baseUrl = $"https://{ctx.EndPoint}";

            var urls = new[]
            {
                $"{baseUrl}/v1/workflows/{id}/versions?include_input_output=true&page_size=1&publish_status=published_online",
                $"{baseUrl}/v1/workflows/{id}/versions?include_input_output=true&page_size=1&publish_status=all",
                $"{baseUrl}/v1/workflows/{id}?connector_id=1024",
                $"{baseUrl}/v1/workflows/{id}"
            };

            foreach (var url in urls)
            {
                var specs = await TryFetchInputSpecsFromUrlAsync(
                    client, ctx.AccessToken, url, cancellationToken);
                if (specs.Count > 0)
                    return specs;
            }

            return [];
        }
        catch
        {
            return [];
        }
    }

    private static async Task<IReadOnlyList<CozeWorkflowInputSpec>> TryFetchInputSpecsFromUrlAsync(
        HttpClient client,
        string accessToken,
        string url,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await client.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            return [];

        using var doc = JsonDocument.Parse(json);
        return TryParseInputSpecsFromRoot(doc.RootElement);
    }

    private static IReadOnlyList<CozeWorkflowInputSpec> TryParseInputSpecsFromRoot(JsonElement root)
    {
        if (root.TryGetProperty("code", out var codeEl) &&
            codeEl.ValueKind == JsonValueKind.Number &&
            codeEl.GetInt64() != 0)
            return [];

        if (!root.TryGetProperty("data", out var data))
            return [];

        var parameters = FindInputParametersElement(data);
        return parameters is null
            ? []
            : CozeWorkflowInputCatalog.ParseFromApiParameters(parameters);
    }

    private static JsonElement? FindInputParametersElement(JsonElement data)
    {
        if (data.TryGetProperty("input", out var directInput) &&
            directInput.TryGetProperty("parameters", out var directParams) &&
            directParams.ValueKind == JsonValueKind.Object &&
            directParams.EnumerateObject().Any())
            return directParams;

        if (data.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in items.EnumerateArray())
            {
                if (item.TryGetProperty("input", out var itemInput) &&
                    itemInput.TryGetProperty("parameters", out var itemParams) &&
                    itemParams.ValueKind == JsonValueKind.Object &&
                    itemParams.EnumerateObject().Any())
                    return itemParams;
            }
        }

        return null;
    }

    /// <summary>上传本地文件到 Coze，返回 file_id（用于工作流文件类参数）。</summary>
    public async Task<string> UploadFileAsync(
        CozeDomainOptions coze,
        GlobalDefaults defaults,
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var ctx = await _clientFactory.CreateContextAsync(coze, defaults, cancellationToken);
        var url = $"https://{ctx.EndPoint}/v1/files/upload";

        using var multipart = new MultipartFormDataContent();
        var streamContent = new StreamContent(content);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        multipart.Add(streamContent, "file", string.IsNullOrWhiteSpace(fileName) ? "file" : fileName);

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ctx.AccessToken);
        request.Content = multipart;

        var client = _httpClientFactory.CreateClient(CozeHttpClientNames.Client);
        using var response = await client.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"Coze 文件上传失败 ({(int)response.StatusCode}): {Truncate(ParseCozeError(json) ?? json, 300)}");

        var envelope = ParseJson<CozeApiEnvelope<CozeFileUploadData>>(json);
        if (envelope?.Code is not null and not 0)
            throw new InvalidOperationException(envelope.Msg ?? $"Coze 文件上传错误码 {envelope.Code}");

        var fileId = envelope?.Data?.Id;
        if (string.IsNullOrWhiteSpace(fileId))
            throw new InvalidOperationException("Coze 文件上传未返回 file_id。");

        return fileId;
    }

    public static string FormatFileParameterValue(string cozeFileId) =>
        JsonSerializer.Serialize(new { file_id = cozeFileId }, JsonOptions);

    public async IAsyncEnumerable<ChatChunk> StreamRunAsync(
        DomainProfile domain,
        GlobalDefaults defaults,
        string workflowId,
        IReadOnlyDictionary<string, string> parameters,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var coze = domain.Coze ?? throw new InvalidOperationException("领域未配置 Coze。");
        var wf = await ResolveWorkflowAsync(domain, defaults, workflowId, cancellationToken)
            ?? throw new InvalidOperationException($"未知工作流: {workflowId}");

        var body = BuildRunBody(coze, wf, parameters);
        await foreach (var chunk in PostWorkflowSseAsync(
            coze, defaults, "/v1/workflow/stream_run", body, workflowId, cancellationToken))
            yield return chunk;
    }

    public async IAsyncEnumerable<ChatChunk> StreamResumeAsync(
        DomainProfile domain,
        GlobalDefaults defaults,
        CozeWorkflowResumeRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var coze = domain.Coze ?? throw new InvalidOperationException("领域未配置 Coze。");
        var body = new Dictionary<string, object?>
        {
            ["workflow_id"] = request.WorkflowId,
            ["event_id"] = request.EventId,
            ["interrupt_type"] = request.InterruptType,
            ["resume_data"] = request.ResumeData
        };

        await foreach (var chunk in PostWorkflowSseAsync(
            coze, defaults, "/v1/workflow/stream_resume", body, request.WorkflowId, cancellationToken))
            yield return chunk;
    }

    private async Task<BotOnlineInfo> GetBotOnlineInfoAsync(
        CozeDomainOptions coze,
        GlobalDefaults defaults,
        CancellationToken cancellationToken)
    {
        var ctx = await _clientFactory.CreateContextAsync(coze, defaults, cancellationToken);

        var url = $"https://{ctx.EndPoint}/v1/bot/get_online_info?bot_id={Uri.EscapeDataString(coze.BotId)}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ctx.AccessToken);

        var client = _httpClientFactory.CreateClient(CozeHttpClientNames.Client);
        using var response = await client.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Coze get_online_info 失败 ({(int)response.StatusCode}): {Truncate(json, 300)}");

        var envelope = JsonSerializer.Deserialize<CozeApiEnvelope<BotOnlineInfoData>>(json, JsonOptions);
        if (envelope?.Code is not null and not 0)
            throw new InvalidOperationException(envelope.Msg ?? $"Coze get_online_info 错误码 {envelope.Code}");

        var data = envelope?.Data ?? new BotOnlineInfoData();
        return new BotOnlineInfo
        {
            Workflows = (data.WorkflowInfo ?? [])
                .Where(w => !string.IsNullOrWhiteSpace(w.Id))
                .Select(w => new BotWorkflowInfo
                {
                    Id = w.Id!,
                    Name = w.Name,
                    Description = w.Description
                })
                .ToList()
        };
    }

    private static Dictionary<string, object?> BuildRunBody(
        CozeDomainOptions coze,
        CozeWorkflowCatalogItem wf,
        IReadOnlyDictionary<string, string> parameters)
    {
        var body = new Dictionary<string, object?>
        {
            ["workflow_id"] = wf.WorkflowId,
            ["parameters"] = JsonSerializer.Serialize(parameters, JsonOptions)
        };

        if (!string.IsNullOrWhiteSpace(wf.AppId))
            body["app_id"] = wf.AppId;
        else if (!string.IsNullOrWhiteSpace(coze.BotId))
            body["bot_id"] = coze.BotId;

        return body;
    }

    private async IAsyncEnumerable<ChatChunk> PostWorkflowSseAsync(
        CozeDomainOptions coze,
        GlobalDefaults defaults,
        string path,
        Dictionary<string, object?> body,
        string workflowId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var ctx = await _clientFactory.CreateContextAsync(coze, defaults, cancellationToken);

        var url = $"https://{ctx.EndPoint}{path}";
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ctx.AccessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        request.Content = new StringContent(
            JsonSerializer.Serialize(body, JsonOptions),
            Encoding.UTF8,
            "application/json");

        var client = _httpClientFactory.CreateClient(CozeHttpClientNames.Client);
        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errText = await response.Content.ReadAsStringAsync(cancellationToken);
            yield return Error(ParseCozeError(errText) ?? $"Coze 工作流请求失败 ({(int)response.StatusCode})");
            yield break;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await foreach (var chunk in ReadCozeWorkflowSseAsync(stream, workflowId, cancellationToken))
            yield return chunk;
    }

    private static async IAsyncEnumerable<ChatChunk> ReadCozeWorkflowSseAsync(
        Stream stream,
        string workflowId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8);
        string? eventName = null;
        var dataLines = new List<string>();

        while (true)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                if (eventName is not null)
                    foreach (var chunk in MapCozeEvent(eventName, string.Join('\n', dataLines), workflowId))
                        yield return chunk;
                yield return new ChatChunk { IsCompleted = true };
                yield break;
            }

            if (line.Length == 0)
            {
                if (eventName is not null)
                {
                    foreach (var chunk in MapCozeEvent(eventName, string.Join('\n', dataLines), workflowId))
                        yield return chunk;
                }
                eventName = null;
                dataLines.Clear();
                continue;
            }

            if (line.StartsWith("event:", StringComparison.OrdinalIgnoreCase))
                eventName = line["event:".Length..].Trim();
            else if (line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                dataLines.Add(line["data:".Length..].Trim());
        }
    }

    private static IEnumerable<ChatChunk> MapCozeEvent(string eventName, string dataJson, string workflowId)
    {
        switch (eventName.Trim())
        {
            case "Message":
                var msg = ParseJson<WorkflowMessageData>(dataJson);
                if (!string.IsNullOrEmpty(msg?.Content))
                    yield return new ChatChunk { TextDelta = msg.Content };
                yield break;

            case "Interrupt":
                var interrupt = ParseJson<WorkflowInterruptData>(dataJson);
                var interruptData = interrupt?.InterruptData;
                if (interruptData is not null && !string.IsNullOrWhiteSpace(interruptData.EventId))
                {
                    yield return new ChatChunk
                    {
                        WorkflowInterrupt = new CozeWorkflowInterruptInfo
                        {
                            WorkflowId = workflowId,
                            EventId = interruptData.EventId,
                            InterruptType = interruptData.Type,
                            NodeTitle = interrupt?.NodeTitle,
                            Prompt = interrupt?.Content
                        }
                    };
                }
                yield break;

            case "Error":
                var err = ParseJson<WorkflowErrorData>(dataJson);
                yield return Error(err?.ErrorMessage ?? dataJson);
                yield break;

            case "Done":
                var done = ParseJson<WorkflowDoneData>(dataJson);
                yield return new ChatChunk
                {
                    IsCompleted = true,
                    WorkflowDebugUrl = done?.DebugUrl
                };
                yield break;

            case "PING":
                yield break;

            default:
                yield break;
        }
    }

    private static T? ParseJson<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return default;
        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch
        {
            return default;
        }
    }

    private static string? ParseCozeError(string json)
    {
        var envelope = ParseJson<CozeApiEnvelope<JsonElement>>(json);
        if (!string.IsNullOrWhiteSpace(envelope?.Msg)) return envelope.Msg;
        var err = ParseJson<WorkflowErrorData>(json);
        return err?.ErrorMessage;
    }

    private static ChatChunk Error(string message) => new() { Error = message, IsCompleted = true };

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "...";

    private sealed class CozeApiEnvelope<T>
    {
        public long? Code { get; set; }
        public string? Msg { get; set; }
        public T? Data { get; set; }
    }

    private sealed class CozeFileUploadData
    {
        public string? Id { get; set; }
    }

    private sealed class WorkflowListData
    {
        [JsonPropertyName("has_more")]
        public bool HasMore { get; set; }

        public List<WorkflowListRow>? Items { get; set; }
    }

    private sealed class WorkflowListRow
    {
        [JsonPropertyName("workflow_id")]
        public string? WorkflowId { get; set; }

        [JsonPropertyName("workflow_name")]
        public string? WorkflowName { get; set; }

        public string? Description { get; set; }

        [JsonPropertyName("icon_url")]
        public string? IconUrl { get; set; }

        [JsonPropertyName("app_id")]
        public string? AppId { get; set; }
    }

    private sealed class BotOnlineInfoData
    {
        [JsonPropertyName("workflow_info")]
        public List<BotWorkflowInfoRow>? WorkflowInfo { get; set; }
    }

    private sealed class BotWorkflowInfoRow
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
    }

    private sealed class BotOnlineInfo
    {
        public required List<BotWorkflowInfo> Workflows { get; init; }
    }

    private sealed class BotWorkflowInfo
    {
        public required string Id { get; init; }
        public string? Name { get; set; }
        public string? Description { get; set; }
    }

    private sealed class WorkflowMessageData
    {
        public string? Content { get; set; }
        public string? NodeTitle { get; set; }
    }

    private sealed class WorkflowInterruptData
    {
        public string? Content { get; set; }
        public string? NodeTitle { get; set; }

        [JsonPropertyName("interrupt_data")]
        public WorkflowInterruptInner? InterruptData { get; set; }
    }

    private sealed class WorkflowInterruptInner
    {
        [JsonPropertyName("event_id")]
        public string? EventId { get; set; }

        public int Type { get; set; }
    }

    private sealed class WorkflowErrorData
    {
        [JsonPropertyName("error_message")]
        public string? ErrorMessage { get; set; }
    }

    private sealed class WorkflowDoneData
    {
        [JsonPropertyName("debug_url")]
        public string? DebugUrl { get; set; }
    }

}

public sealed class CozeWorkflowCatalogItem
{
    public required string WorkflowId { get; init; }
    public required string DisplayName { get; init; }
    public string? Description { get; init; }
    public string? IconUrl { get; init; }
    public string InputParameter { get; init; } = "BOT_USER_INPUT";
    public string? AppId { get; init; }
    public string? InputHint { get; init; }
    public string? InputSummary { get; init; }
    public bool NeedsAttachment { get; init; }
    public IReadOnlyList<CozeWorkflowInputSpec> Inputs { get; init; } = [];
}

public sealed class CozeWorkflowResumeRequest
{
    public required string WorkflowId { get; init; }
    public required string EventId { get; init; }
    public required int InterruptType { get; init; }
    public required string ResumeData { get; init; }
}
