using DataChat.Core.Abstractions;
using DataChat.Core.Entities;
using Microsoft.Data.Sqlite;

namespace DataChat.Infrastructure.Persistence;

public sealed class SqliteConversationRepository : IConversationRepository, IDatabaseInitializer, IAsyncDisposable
{
    private readonly string _connectionString;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    public SqliteConversationRepository(string databasePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
        _connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath }.ConnectionString;
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_initialized) return;
        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_initialized) return;
            await InitializeAsync(cancellationToken);
            _initialized = true;
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
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = string.IsNullOrWhiteSpace(userId)
            ? "SELECT id,title,domain_id,chat_mode,model,resource_id,created_at,updated_at,user_id FROM chat_session ORDER BY updated_at DESC"
            : "SELECT id,title,domain_id,chat_mode,model,resource_id,created_at,updated_at,user_id FROM chat_session WHERE user_id=$uid ORDER BY updated_at DESC";
        if (!string.IsNullOrWhiteSpace(userId))
            cmd.Parameters.AddWithValue("$uid", userId);
        var list = new List<ChatSession>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            list.Add(ReadSession(reader));
        return list;
    }

    public async Task<ChatSession?> GetSessionAsync(string id, string? userId = null, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = string.IsNullOrWhiteSpace(userId)
            ? "SELECT id,title,domain_id,chat_mode,model,resource_id,created_at,updated_at,user_id FROM chat_session WHERE id=$id"
            : "SELECT id,title,domain_id,chat_mode,model,resource_id,created_at,updated_at,user_id FROM chat_session WHERE id=$id AND user_id=$uid";
        cmd.Parameters.AddWithValue("$id", id);
        if (!string.IsNullOrWhiteSpace(userId))
            cmd.Parameters.AddWithValue("$uid", userId);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadSession(reader) : null;
    }

    public async Task SaveSessionAsync(ChatSession session, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO chat_session(id,title,domain_id,chat_mode,model,resource_id,created_at,updated_at,user_id)
            VALUES($id,$title,$domain,$mode,$model,$resource,$created,$updated,$uid)
            ON CONFLICT(id) DO UPDATE SET
              title=$title, domain_id=$domain, chat_mode=$mode, model=$model,
              resource_id=$resource, updated_at=$updated, user_id=$uid
            """;
        BindSession(cmd, session);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteSessionAsync(string id, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM chat_message WHERE session_id=$id; DELETE FROM chat_session WHERE id=$id";
        cmd.Parameters.AddWithValue("$id", id);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id,session_id,role,content,extras_json,created_at FROM chat_message WHERE session_id=$sid ORDER BY created_at";
        cmd.Parameters.AddWithValue("$sid", sessionId);
        var list = new List<ChatMessage>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            list.Add(ReadMessage(reader));
        return list;
    }

    public async Task AppendMessageAsync(ChatMessage message, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO chat_message(id,session_id,role,content,extras_json,created_at) VALUES($id,$sid,$role,$content,$extras,$created)";
        cmd.Parameters.AddWithValue("$id", message.Id);
        cmd.Parameters.AddWithValue("$sid", message.SessionId);
        cmd.Parameters.AddWithValue("$role", message.Role);
        cmd.Parameters.AddWithValue("$content", message.Content);
        cmd.Parameters.AddWithValue("$extras", (object?)message.ExtrasJson ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$created", message.CreatedAt);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateMessageContentAsync(string messageId, string content, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE chat_message SET content=$content WHERE id=$id";
        cmd.Parameters.AddWithValue("$id", messageId);
        cmd.Parameters.AddWithValue("$content", content);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task ClearMessagesAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM chat_message WHERE session_id=$sid";
        cmd.Parameters.AddWithValue("$sid", sessionId);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static ChatSession ReadSession(SqliteDataReader r) => new()
    {
        Id = r.GetString(0),
        Title = r.GetString(1),
        DomainId = r.GetString(2),
        ChatMode = r.GetString(3),
        Model = r.IsDBNull(4) ? null : r.GetString(4),
        ResourceId = r.IsDBNull(5) ? null : r.GetString(5),
        CreatedAt = r.GetInt64(6),
        UpdatedAt = r.GetInt64(7),
        UserId = r.FieldCount > 8 && !r.IsDBNull(8) ? r.GetString(8) : null
    };

    private static ChatMessage ReadMessage(SqliteDataReader r) => new()
    {
        Id = r.GetString(0),
        SessionId = r.GetString(1),
        Role = r.GetString(2),
        Content = r.GetString(3),
        ExtrasJson = r.IsDBNull(4) ? null : r.GetString(4),
        CreatedAt = r.GetInt64(5)
    };

    private static void BindSession(SqliteCommand cmd, ChatSession s)
    {
        cmd.Parameters.AddWithValue("$id", s.Id);
        cmd.Parameters.AddWithValue("$title", s.Title);
        cmd.Parameters.AddWithValue("$domain", s.DomainId);
        cmd.Parameters.AddWithValue("$mode", s.ChatMode);
        cmd.Parameters.AddWithValue("$model", (object?)s.Model ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$resource", (object?)s.ResourceId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$created", s.CreatedAt);
        cmd.Parameters.AddWithValue("$updated", s.UpdatedAt);
        cmd.Parameters.AddWithValue("$uid", (object?)s.UserId ?? DBNull.Value);
    }
}
