using System.Collections.Concurrent;
using DataChat.Core.Abstractions;

namespace DataChat.Providers.Coze;

/// <summary>进程内 Coze 会话映射；Gateway 多实例部署时应替换为 Redis 实现。</summary>
public sealed class InMemoryCozeConversationStore : ICozeConversationStore
{
    private readonly ConcurrentDictionary<string, string> _map = new();

    private static string Key(string domainId, string sessionId) => $"{domainId}:{sessionId}";

    public Task<string?> GetConversationIdAsync(string domainId, string sessionId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_map.TryGetValue(Key(domainId, sessionId), out var id) ? id : null);

    public Task SetConversationIdAsync(string domainId, string sessionId, string conversationId, CancellationToken cancellationToken = default)
    {
        _map[Key(domainId, sessionId)] = conversationId;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string domainId, string sessionId, CancellationToken cancellationToken = default)
    {
        _map.TryRemove(Key(domainId, sessionId), out _);
        return Task.CompletedTask;
    }
}
