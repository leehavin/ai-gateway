using DataChat.Core.Abstractions;
using DataChat.Core.Chat;
using DataChat.Core.Configuration;
using DataChat.Core.Entities;
using DataChat.Gateway.Configuration;
using DataChat.Gateway.Models;
using Microsoft.Extensions.Options;

namespace DataChat.Gateway.Services;

public sealed class GatewaySessionService
{
    private readonly IConversationRepository _repository;
    private readonly IDomainCatalog _domains;
    private readonly GatewayOptions _options;

    public GatewaySessionService(
        IConversationRepository repository,
        IDomainCatalog domains,
        IOptions<GatewayOptions> options)
    {
        _repository = repository;
        _domains = domains;
        _options = options.Value;
    }

    public bool IsEnabled => _options.EnableSessionApi;

    public async Task<IReadOnlyList<SessionSummaryDto>> ListAsync(
        string? domainId,
        CancellationToken cancellationToken)
    {
        var sessions = await _repository.ListSessionsAsync(cancellationToken);
        var query = sessions.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(domainId))
            query = query.Where(s => string.Equals(s.DomainId, domainId, StringComparison.OrdinalIgnoreCase));

        var list = new List<SessionSummaryDto>();
        foreach (var s in query)
        {
            var messages = await _repository.GetMessagesAsync(s.Id, cancellationToken);
            var preview = messages.LastOrDefault(m => m.Role == "assistant")?.Content
                ?? messages.LastOrDefault(m => m.Role == "user")?.Content;
            list.Add(new SessionSummaryDto
            {
                Id = s.Id,
                Title = s.Title,
                DomainId = s.DomainId,
                UpdatedAt = s.UpdatedAt,
                Preview = preview is null ? null : preview.Length > 80 ? preview[..80] : preview
            });
        }

        return list.OrderByDescending(x => x.UpdatedAt).ToList();
    }

    public async Task<SessionDetailDto?> GetAsync(string id, CancellationToken cancellationToken)
    {
        var session = await _repository.GetSessionAsync(id, cancellationToken);
        if (session is null) return null;
        var messages = await _repository.GetMessagesAsync(id, cancellationToken);
        return new SessionDetailDto
        {
            Id = session.Id,
            Title = session.Title,
            DomainId = session.DomainId,
            UpdatedAt = session.UpdatedAt,
            Messages = messages.Select(m => new SessionMessageDto
            {
                Id = m.Id,
                Role = m.Role,
                Content = m.Content,
                ExtrasJson = m.ExtrasJson,
                CreatedAt = m.CreatedAt
            }).ToList()
        };
    }

    public async Task<SessionDetailDto> CreateAsync(CreateSessionRequest request, CancellationToken cancellationToken)
    {
        var domain = _domains.Current.Domains.FirstOrDefault(d =>
            string.Equals(d.Id, request.DomainId, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException("未知领域: " + request.DomainId);

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var session = new ChatSession
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = string.IsNullOrWhiteSpace(request.Title) ? domain.DisplayName : request.Title.Trim(),
            DomainId = domain.Id,
            ChatMode = domain.ChatMode,
            Model = domain.Model,
            ResourceId = domain.Dbgpt?.AppId ?? domain.Dbgpt?.DatasourceId,
            CreatedAt = now,
            UpdatedAt = now
        };
        await _repository.SaveSessionAsync(session, cancellationToken);
        return new SessionDetailDto
        {
            Id = session.Id,
            Title = session.Title,
            DomainId = session.DomainId,
            UpdatedAt = session.UpdatedAt,
            Messages = []
        };
    }

    public async Task<SessionDetailDto?> SyncAsync(
        string id,
        SyncSessionRequest request,
        CancellationToken cancellationToken)
    {
        var domain = _domains.Current.Domains.FirstOrDefault(d =>
            string.Equals(d.Id, request.DomainId, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException("未知领域: " + request.DomainId);

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var existing = await _repository.GetSessionAsync(id, cancellationToken);
        var session = new ChatSession
        {
            Id = id,
            Title = string.IsNullOrWhiteSpace(request.Title)
                ? existing?.Title ?? domain.DisplayName
                : request.Title.Trim(),
            DomainId = domain.Id,
            ChatMode = domain.ChatMode,
            Model = domain.Model,
            ResourceId = domain.Dbgpt?.AppId ?? domain.Dbgpt?.DatasourceId,
            CreatedAt = existing?.CreatedAt ?? now,
            UpdatedAt = now
        };
        await _repository.SaveSessionAsync(session, cancellationToken);

        await _repository.ClearMessagesAsync(id, cancellationToken);
        var messages = request.Messages ?? [];
        for (var i = 0; i < messages.Count; i++)
        {
            var m = messages[i];
            await _repository.AppendMessageAsync(new ChatMessage
            {
                Id = string.IsNullOrWhiteSpace(m.Id) ? Guid.NewGuid().ToString("N") : m.Id,
                SessionId = id,
                Role = m.Role,
                Content = m.Content ?? "",
                ExtrasJson = m.ExtrasJson,
                CreatedAt = m.CreatedAt > 0 ? m.CreatedAt : now + i
            }, cancellationToken);
        }

        return await GetAsync(id, cancellationToken);
    }

    public Task DeleteAsync(string id, CancellationToken cancellationToken) =>
        _repository.DeleteSessionAsync(id, cancellationToken);

    public async Task PersistStreamTurnAsync(
        ChatStreamRequest request,
        string userMessage,
        string assistantContent,
        string? thinkingContent,
        IReadOnlyList<ChatCitation>? citations,
        CancellationToken cancellationToken)
    {
        if (!_options.PersistSessions) return;
        if (string.IsNullOrWhiteSpace(request.SessionId)) return;

        var domain = _domains.Current.Domains.FirstOrDefault(d =>
            string.Equals(d.Id, request.Domain, StringComparison.OrdinalIgnoreCase));
        if (domain is null) return;

        var sessionId = request.SessionId;
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var existing = await _repository.GetSessionAsync(sessionId, cancellationToken);
        var title = existing?.Title ?? domain.DisplayName;
        if ((existing is null || title == domain.DisplayName) && !string.IsNullOrWhiteSpace(request.Message))
        {
            var t = request.Message.Trim();
            title = t.Length > 36 ? t[..36] + "…" : t;
        }

        var session = new ChatSession
        {
            Id = sessionId,
            Title = title,
            DomainId = domain.Id,
            ChatMode = domain.ChatMode,
            Model = domain.Model,
            ResourceId = domain.Dbgpt?.AppId ?? domain.Dbgpt?.DatasourceId,
            CreatedAt = existing?.CreatedAt ?? now,
            UpdatedAt = now
        };
        await _repository.SaveSessionAsync(session, cancellationToken);

        var extras = BuildExtrasJson(thinkingContent, citations);
        await _repository.AppendMessageAsync(new ChatMessage
        {
            Id = Guid.NewGuid().ToString("N"),
            SessionId = sessionId,
            Role = "user",
            Content = userMessage,
            CreatedAt = now
        }, cancellationToken);

        await _repository.AppendMessageAsync(new ChatMessage
        {
            Id = Guid.NewGuid().ToString("N"),
            SessionId = sessionId,
            Role = "assistant",
            Content = assistantContent,
            ExtrasJson = extras,
            CreatedAt = now + 1
        }, cancellationToken);
    }

    private static string? BuildExtrasJson(string? thinking, IReadOnlyList<ChatCitation>? citations)
    {
        if (string.IsNullOrWhiteSpace(thinking) && citations is not { Count: > 0 }) return null;
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            thinking,
            citations = citations?.Select(c => new { c.Title, c.Url, c.Snippet })
        });
    }
}
