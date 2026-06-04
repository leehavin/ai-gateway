using DataChat.Core.Abstractions;
using DataChat.Core.Chat;
using DataChat.Core.Configuration;
using DataChat.Core.Entities;
using DataChat.Core.History;

namespace DataChat.WinForms;

public sealed class ChatService
{
    private readonly ChatOrchestrator _orchestrator;
    private readonly IConversationRepository _repository;
    private readonly IDomainCatalog _domains;
    private CancellationTokenSource? _currentCts;

    public ChatService(
        ChatOrchestrator orchestrator,
        IConversationRepository repository,
        IDomainCatalog domains)
    {
        _orchestrator = orchestrator;
        _repository = repository;
        _domains = domains;
    }

    public DomainsConfiguration Domains => _domains.Current;

    public Task<IReadOnlyList<ChatSession>> ListSessionsAsync(CancellationToken ct = default) =>
        _repository.ListSessionsAsync(ct);

    public async Task<ChatSession> CreateSessionAsync(string domainId, CancellationToken ct = default)
    {
        var domain = _domains.Current.Domains.First(d => d.Id == domainId);
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var session = new ChatSession
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = domain.DisplayName,
            DomainId = domain.Id,
            ChatMode = domain.ChatMode,
            Model = domain.Model,
            ResourceId = domain.Dbgpt?.AppId ?? domain.Dbgpt?.DatasourceId,
            CreatedAt = now,
            UpdatedAt = now
        };
        await _repository.SaveSessionAsync(session, ct);
        return session;
    }

    public Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(string sessionId, CancellationToken ct = default) =>
        _repository.GetMessagesAsync(sessionId, ct);

    public void CancelCurrent() => _currentCts?.Cancel();

    public async IAsyncEnumerable<ChatChunk> SendMessageAsync(
        string sessionId,
        string userText,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken externalCt = default)
    {
        var session = await _repository.GetSessionAsync(sessionId, externalCt)
            ?? throw new InvalidOperationException("会话不存在");
        var domain = _domains.Current.Domains.First(d => d.Id == session.DomainId);
        var history = await _repository.GetMessagesAsync(sessionId, externalCt);
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var userMsg = new ChatMessage
        {
            Id = Guid.NewGuid().ToString("N"),
            SessionId = sessionId,
            Role = "user",
            Content = userText,
            CreatedAt = now
        };
        await _repository.AppendMessageAsync(userMsg, externalCt);

        var assistantMsg = new ChatMessage
        {
            Id = Guid.NewGuid().ToString("N"),
            SessionId = sessionId,
            Role = "assistant",
            Content = "",
            CreatedAt = now + 1
        };
        await _repository.AppendMessageAsync(assistantMsg, externalCt);

        _currentCts?.Cancel();
        _currentCts = CancellationTokenSource.CreateLinkedTokenSource(externalCt);
        var ct = _currentCts.Token;

        var context = new ChatContext
        {
            Session = session,
            Domain = domain,
            History = history,
            UserMessage = userText
        };

        var buffer = new System.Text.StringBuilder();
        await foreach (var chunk in _orchestrator.SendAsync(context, ct))
        {
            if (chunk.Error is not null)
            {
                yield return chunk;
                break;
            }
            if (chunk.TextDelta is not null)
            {
                buffer.Append(chunk.TextDelta);
                yield return chunk;
            }
            if (chunk.IsCompleted)
                break;
        }

        await _repository.UpdateMessageContentAsync(assistantMsg.Id, buffer.ToString(), CancellationToken.None);
        session.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (session.Title == domain.DisplayName && buffer.Length > 0)
            session.Title = buffer.ToString()[..Math.Min(24, buffer.Length)];
        await _repository.SaveSessionAsync(session, CancellationToken.None);
        yield return new ChatChunk { IsCompleted = true };
    }
}
