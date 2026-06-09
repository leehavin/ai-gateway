using System.Runtime.CompilerServices;
using System.Text.Json;
using DataChat.Core.Abstractions;
using DataChat.Core.Chat;
using DataChat.Core.Configuration;
using DataChat.Core.History;
using DataChat.Providers.Sse;
using Microsoft.Extensions.Http;

namespace DataChat.Providers.Dbgpt;

/// <summary>
/// 公司统一 DB-GPT 服务器；对齐官方 POST /api/v2/chat/completions。
/// </summary>
public sealed class DbGptChatProvider : IChatProvider
{
    private readonly DbgptApiClient _api;

    public DbGptChatProvider(
        IHttpClientFactory httpClientFactory,
        IApiKeyStore apiKeyStore,
        string baseUrl)
    {
        _api = new DbgptApiClient(httpClientFactory, apiKeyStore, baseUrl);
    }

    public string ProviderId => "dbgpt";

    public bool CanHandle(DomainProfile domain) =>
        domain.Provider.Equals("dbgpt", StringComparison.OrdinalIgnoreCase);

    public async IAsyncEnumerable<ChatChunk> StreamChatAsync(
        ChatContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var dbgpt = context.Domain.Dbgpt;
        var chatMode = dbgpt?.ChatMode ?? "chat_app";
        var chatParam = dbgpt?.AppId ?? dbgpt?.DatasourceId ?? dbgpt?.KnowledgeSpaceName ?? "";

        var history = ChatHistoryBuilder.Build(context.Domain, context.History, context.UserMessage, 20);
        var model = context.Domain.Model ?? context.Session.Model ?? "gpt-4o";

        // chat_data / chat_dashboard：DB-GPT 流式只返回 Agent JSON，图表仅在非流式 + enable_vis 的 &lt;chart-view&gt; 里。
        if (NeedsVisResponse(chatMode))
        {
            await foreach (var chunk in StreamVisChatAsync(model, history, context.UserMessage, chatMode, chatParam, cancellationToken))
                yield return chunk;
            yield break;
        }

        var body = DbgptRequestBuilder.BuildCompletionBody(
            model, history, context.UserMessage, chatMode, chatParam, stream: true);

        using var response = await _api.PostStreamingAsync("/api/v2/chat/completions", body, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errText = await response.Content.ReadAsStringAsync(cancellationToken);
            yield return new ChatChunk
            {
                Error = $"DB-GPT 错误 {(int)response.StatusCode}：{Truncate(errText, 300)}",
                IsCompleted = true
            };
            yield break;
        }

        await foreach (var line in SseLineReader.ReadDataLinesAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken))
        {
            var delta = SseLineReader.TryExtractOpenAiDelta(line);
            if (!string.IsNullOrEmpty(delta))
                yield return new ChatChunk { TextDelta = delta };
        }

        yield return new ChatChunk { IsCompleted = true };
    }

    private static bool NeedsVisResponse(string chatMode) =>
        chatMode is "chat_data" or "chat_dashboard";

    private async IAsyncEnumerable<ChatChunk> StreamVisChatAsync(
        string model,
        IReadOnlyList<HistoryMessage> history,
        string userMessage,
        string chatMode,
        string? chatParam,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var body = DbgptRequestBuilder.BuildCompletionBody(
            model, history, userMessage, chatMode, chatParam, stream: false);

        using var response = await _api.PostAsync("/api/v2/chat/completions", body, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errText = await response.Content.ReadAsStringAsync(cancellationToken);
            yield return new ChatChunk
            {
                Error = $"DB-GPT 错误 {(int)response.StatusCode}：{Truncate(errText, 300)}",
                IsCompleted = true
            };
            yield break;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var formatted = ExtractAndFormatVisContent(json);
        if (string.IsNullOrWhiteSpace(formatted))
        {
            yield return new ChatChunk { Error = "DB-GPT 未返回可视化内容。", IsCompleted = true };
            yield break;
        }

        // 非流式拿到完整结果后，按块推给前端以保持打字机效果。
        const int chunkSize = 24;
        for (var i = 0; i < formatted.Length; i += chunkSize)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var len = Math.Min(chunkSize, formatted.Length - i);
            yield return new ChatChunk { TextDelta = formatted.Substring(i, len) };
            await Task.Delay(8, cancellationToken);
        }

        yield return new ChatChunk { IsCompleted = true };
    }

    internal static string ExtractAndFormatVisContent(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("choices", out var choices)
                && choices.GetArrayLength() > 0
                && choices[0].TryGetProperty("message", out var message)
                && message.TryGetProperty("content", out var content))
            {
                return DbgptVisFormatter.FormatAssistantContent(content.GetString() ?? "");
            }
        }
        catch (JsonException)
        {
            // fall through
        }

        return json;
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "...";
}
