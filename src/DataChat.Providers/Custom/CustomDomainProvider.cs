using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using DataChat.Core.Abstractions;
using DataChat.Core.Chat;
using DataChat.Core.Configuration;
using DataChat.Core.History;
using DataChat.Providers.Sse;
using Microsoft.Extensions.Http;

namespace DataChat.Providers.Custom;

/// <summary>
/// 主路径：公司自研 HTTP/SSE 领域服务（§8.1 统一契约）。
/// </summary>
public sealed class CustomDomainProvider : IChatProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IApiKeyStore _apiKeyStore;

    public CustomDomainProvider(IHttpClientFactory httpClientFactory, IApiKeyStore apiKeyStore)
    {
        _httpClientFactory = httpClientFactory;
        _apiKeyStore = apiKeyStore;
    }

    public string ProviderId => "custom";

    public bool CanHandle(DomainProfile domain) =>
        domain.Provider.Equals("custom", StringComparison.OrdinalIgnoreCase)
        && domain.Custom is not null;

    public async IAsyncEnumerable<ChatChunk> StreamChatAsync(
        ChatContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var custom = context.Domain.Custom!;
        var apiKey = await _apiKeyStore.GetAsync(custom.ApiKeyRef, cancellationToken);

        var history = ChatHistoryBuilder.Build(
            context.Domain,
            context.History,
            context.UserMessage,
            maxTurns: 20);

        var payload = new Dictionary<string, object?>
        {
            ["sessionId"] = context.Session.Id,
            ["domain"] = context.Domain.Id,
            ["message"] = context.UserMessage,
            ["stream"] = true,
            ["messages"] = history.Select(h => new { role = h.Role, content = h.Content }).ToList()
        };
        if (context.Parameters is not null)
        {
            payload["parameters"] = new
            {
                temperature = context.Parameters.Temperature,
                topP = context.Parameters.TopP,
                maxTokens = context.Parameters.MaxTokens
            };
        }

        var client = _httpClientFactory.CreateClient("DataChat");
        using var request = new HttpRequestMessage(HttpMethod.Post, custom.Endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

        ApplyAuth(request, custom, apiKey);

        using var response = await client.SendAsync(
            request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            yield return new ChatChunk { Error = $"领域服务错误 {(int)response.StatusCode}", IsCompleted = true };
            yield break;
        }

        await foreach (var line in SseLineReader.ReadDataLinesAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken))
        {
            var delta = SseLineReader.TryExtractOpenAiDelta(line)
                        ?? TryExtractSimpleDelta(line);
            if (!string.IsNullOrEmpty(delta))
                yield return new ChatChunk { TextDelta = delta };
        }

        yield return new ChatChunk { IsCompleted = true };
    }

    private static void ApplyAuth(HttpRequestMessage request, CustomDomainOptions custom, string? apiKey)
    {
        if (string.IsNullOrEmpty(apiKey)) return;
        if (!string.IsNullOrEmpty(custom.AuthHeaderName))
            request.Headers.TryAddWithoutValidation(custom.AuthHeaderName, apiKey);
        else
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
    }

    private static string? TryExtractSimpleDelta(string jsonLine)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonLine);
            if (doc.RootElement.TryGetProperty("delta", out var delta))
                return delta.GetString();
            if (doc.RootElement.TryGetProperty("content", out var content))
                return content.GetString();
        }
        catch (JsonException) { }
        return null;
    }
}
