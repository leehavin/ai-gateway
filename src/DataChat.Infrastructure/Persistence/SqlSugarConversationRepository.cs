using DataChat.Core.Abstractions;
using DataChat.Core.Entities;
using SqlSugar;

namespace DataChat.Infrastructure.Persistence;

public sealed class SqlSugarConversationRepository : IConversationRepository, IDatabaseInitializer, IAsyncDisposable
{
    private readonly ISqlSugarClient _db;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    public SqlSugarConversationRepository(ISqlSugarClient db) => _db = db;

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_initialized) return;
        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_initialized) return;
            await InitializeAsync(cancellationToken);
        }
        finally
        {
            _initLock.Release();
        }
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _initialized = true;
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<ChatSession>> ListSessionsAsync(string? userId = null, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        var query = _db.Queryable<ChatSessionEntity>();
        if (!string.IsNullOrWhiteSpace(userId))
            query = query.Where(x => x.UserId == userId);
        var rows = await query.OrderBy(x => x.UpdatedAt, OrderByType.Desc).ToListAsync(cancellationToken);
        return rows.Select(ToSession).ToList();
    }

    public async Task<ChatSession?> GetSessionAsync(string id, string? userId = null, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        var query = _db.Queryable<ChatSessionEntity>().Where(x => x.Id == id);
        if (!string.IsNullOrWhiteSpace(userId))
            query = query.Where(x => x.UserId == userId);
        var row = await query.FirstAsync(cancellationToken);
        return row is null ? null : ToSession(row);
    }

    public async Task SaveSessionAsync(ChatSession session, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        var row = ToEntity(session);
        await _db.Storageable(row).WhereColumns(x => x.Id).ExecuteCommandAsync(cancellationToken);
    }

    public async Task DeleteSessionAsync(string id, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await _db.Deleteable<ChatMessageEntity>().Where(m => m.SessionId == id).ExecuteCommandAsync(cancellationToken);
        await _db.Deleteable<ChatSessionEntity>().Where(s => s.Id == id).ExecuteCommandAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        var rows = await _db.Queryable<ChatMessageEntity>()
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
        return rows.Select(ToMessage).ToList();
    }

    public async Task AppendMessageAsync(ChatMessage message, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await _db.Insertable(ToEntity(message)).ExecuteCommandAsync(cancellationToken);
    }

    public async Task UpdateMessageContentAsync(string messageId, string content, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await _db.Updateable<ChatMessageEntity>()
            .SetColumns(m => m.Content == content)
            .Where(m => m.Id == messageId)
            .ExecuteCommandAsync(cancellationToken);
    }

    public async Task ClearMessagesAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await _db.Deleteable<ChatMessageEntity>().Where(m => m.SessionId == sessionId).ExecuteCommandAsync(cancellationToken);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static ChatSession ToSession(ChatSessionEntity e) => new()
    {
        Id = e.Id,
        Title = e.Title,
        DomainId = e.DomainId,
        ChatMode = e.ChatMode,
        Model = e.Model,
        ResourceId = e.ResourceId,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
        UserId = e.UserId
    };

    private static ChatSessionEntity ToEntity(ChatSession s) => new()
    {
        Id = s.Id,
        Title = s.Title,
        DomainId = s.DomainId,
        ChatMode = s.ChatMode,
        Model = s.Model,
        ResourceId = s.ResourceId,
        CreatedAt = s.CreatedAt,
        UpdatedAt = s.UpdatedAt,
        UserId = s.UserId
    };

    private static ChatMessage ToMessage(ChatMessageEntity e) => new()
    {
        Id = e.Id,
        SessionId = e.SessionId,
        Role = e.Role,
        Content = e.Content,
        ExtrasJson = e.ExtrasJson,
        CreatedAt = e.CreatedAt
    };

    private static ChatMessageEntity ToEntity(ChatMessage m) => new()
    {
        Id = m.Id,
        SessionId = m.SessionId,
        Role = m.Role,
        Content = m.Content,
        ExtrasJson = m.ExtrasJson,
        CreatedAt = m.CreatedAt
    };
}
