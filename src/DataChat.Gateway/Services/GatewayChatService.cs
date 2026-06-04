using DataChat.Core.Abstractions;
using DataChat.Core.Chat;
using DataChat.Core.Configuration;
using DataChat.Core.Entities;
using DataChat.Gateway.Configuration;
using DataChat.Gateway.Models;
using Microsoft.Extensions.Options;

namespace DataChat.Gateway.Services;

public sealed class GatewayChatService
{
    private readonly ChatOrchestrator _orchestrator;
    private readonly IDomainCatalog _domains;
    private readonly GatewayOptions _options;
    private readonly FileStorageService _files;
    private readonly GatewaySessionService _sessions;
    private readonly ILogger<GatewayChatService> _logger;

    public GatewayChatService(
        ChatOrchestrator orchestrator,
        IDomainCatalog domains,
        IOptions<GatewayOptions> options,
        FileStorageService files,
        GatewaySessionService sessions,
        ILogger<GatewayChatService> logger)
    {
        _orchestrator = orchestrator;
        _domains = domains;
        _options = options.Value;
        _files = files;
        _sessions = sessions;
        _logger = logger;
    }

    public DomainProfile? ResolveDomain(string domainId) =>
        _domains.Current.Domains.FirstOrDefault(d => string.Equals(d.Id, domainId, StringComparison.OrdinalIgnoreCase));

    public string? ValidateRequest(ChatStreamRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Domain))
            return "domain 不能为空。";

        var hasText = !string.IsNullOrWhiteSpace(request.Message);
        var hasFiles = request.Attachments is { Count: > 0 };
        if (!hasText && !hasFiles)
            return "message 或 attachments 至少提供一项。";

        if (hasFiles)
        {
            foreach (var att in request.Attachments!)
            {
                if (string.IsNullOrWhiteSpace(att.FileId))
                    return "attachments.fileId 不能为空。";
                if (_files.Get(att.FileId) is null)
                    return "附件不存在或已过期: " + att.FileId;
            }
        }

        var composed = ComposeUserMessage(request);
        if (composed.Length > _options.MaxMessageLength)
            return $"message 超过最大长度 {_options.MaxMessageLength}。";
        if (ResolveDomain(request.Domain) is null)
            return "未知领域: " + request.Domain;
        return null;
    }

    public ChatContext BuildContext(ChatStreamRequest request, DomainProfile domain)
    {
        var userMessage = ComposeUserMessage(request);
        var session = new ChatSession
        {
            Id = string.IsNullOrWhiteSpace(request.SessionId) ? Guid.NewGuid().ToString("N") : request.SessionId,
            Title = domain.DisplayName,
            DomainId = domain.Id,
            ChatMode = domain.ChatMode,
            Model = domain.Model,
            ResourceId = domain.Dbgpt?.AppId ?? domain.Dbgpt?.DatasourceId,
            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        var maxTurns = _domains.Current.Defaults.MaxHistoryTurns > 0
            ? _domains.Current.Defaults.MaxHistoryTurns
            : 20;
        var history = TrimHistory(request.Messages ?? [], userMessage, maxTurns);

        return new ChatContext
        {
            Session = session,
            Domain = domain,
            History = history,
            UserMessage = userMessage,
            Parameters = MapParameters(request.Parameters)
        };
    }

    private static ChatGenerationParameters? MapParameters(ChatParametersDto? dto)
    {
        if (dto is null) return null;
        if (dto.Temperature is null && dto.TopP is null && dto.MaxTokens is null) return null;
        return new ChatGenerationParameters
        {
            Temperature = Clamp(dto.Temperature, 0, 2),
            TopP = Clamp(dto.TopP, 0, 1),
            MaxTokens = dto.MaxTokens is > 0 and <= 128000 ? dto.MaxTokens : null
        };
    }

    private static double? Clamp(double? value, double min, double max) =>
        value is null ? null : Math.Clamp(value.Value, min, max);

    private string ComposeUserMessage(ChatStreamRequest request) =>
        AttachmentMessageComposer.Compose(request.Message ?? "", request.Attachments, _files);

    public async IAsyncEnumerable<ChatChunk> StreamAsync(
        ChatContext context,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Chat stream domain={Domain} session={SessionId} mock={UseMock}",
            context.Domain.Id, context.Session.Id, _options.UseMock);

        await foreach (var chunk in _orchestrator.SendAsync(context, cancellationToken))
            yield return chunk;
    }

    public async IAsyncEnumerable<ChatChunk> StreamAndPersistAsync(
        ChatStreamRequest request,
        ChatContext context,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var text = new System.Text.StringBuilder();
        var thinking = new System.Text.StringBuilder();
        IReadOnlyList<ChatCitation>? citations = null;

        // try/finally 确保即使消费者提前 break（如 SseResponseWriter 遇到 error chunk），
        // DisposeAsync 触发时 finally 仍会执行持久化。
        try
        {
            await foreach (var chunk in StreamAsync(context, cancellationToken))
            {
                if (chunk.TextDelta is not null) text.Append(chunk.TextDelta);
                if (chunk.ThinkingDelta is not null) thinking.Append(chunk.ThinkingDelta);
                if (chunk.Citations is not null) citations = chunk.Citations;
                yield return chunk;
            }
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(context.UserMessage))
            {
                // CancellationToken.None：客户端断开不应中断持久化
                await _sessions.PersistStreamTurnAsync(
                    request,
                    context.UserMessage,
                    text.ToString(),
                    thinking.Length > 0 ? thinking.ToString() : null,
                    citations,
                    CancellationToken.None);
            }
        }
    }

    private static List<ChatMessage> TrimHistory(
        IReadOnlyList<ChatStreamMessage> messages,
        string currentUserMessage,
        int maxTurns)
    {
        var list = messages
            .Where(m => m.Role is "user" or "assistant" or "system")
            .Select((m, i) => new ChatMessage
            {
                Id = "h-" + i,
                SessionId = "remote",
                Role = m.Role,
                Content = m.Content ?? "",
                CreatedAt = i
            })
            .ToList();

        // 当前轮次由 UserMessage 承载（含附件拼接），客户端 messages 末尾可能含展示用 user 文本
        if (list.Count > 0 && list[^1].Role == "user")
            list = list.Take(list.Count - 1).ToList();

        var system = list.Where(m => m.Role == "system").ToList();
        var dialogue = list.Where(m => m.Role is "user" or "assistant").TakeLast(maxTurns * 2).ToList();

        var merged = new List<ChatMessage>();
        merged.AddRange(system.Take(1));
        merged.AddRange(dialogue);
        return merged;
    }
}
