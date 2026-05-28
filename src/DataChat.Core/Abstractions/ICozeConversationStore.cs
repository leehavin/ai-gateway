namespace DataChat.Core.Abstractions;

/// <summary>
/// DataChat sessionId 与 Coze conversationId 映射（Gateway 进程内；生产可换 Redis 实现）。
/// </summary>
public interface ICozeConversationStore
{
    Task<string?> GetConversationIdAsync(string domainId, string sessionId, CancellationToken cancellationToken = default);
    Task SetConversationIdAsync(string domainId, string sessionId, string conversationId, CancellationToken cancellationToken = default);
    Task RemoveAsync(string domainId, string sessionId, CancellationToken cancellationToken = default);
}
