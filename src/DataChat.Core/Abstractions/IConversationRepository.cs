using DataChat.Core.Entities;

namespace DataChat.Core.Abstractions;

public interface IConversationRepository
{
    Task<IReadOnlyList<ChatSession>> ListSessionsAsync(string? userId = null, CancellationToken cancellationToken = default);
    Task<ChatSession?> GetSessionAsync(string id, string? userId = null, CancellationToken cancellationToken = default);
    Task SaveSessionAsync(ChatSession session, CancellationToken cancellationToken = default);
    Task DeleteSessionAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(string sessionId, CancellationToken cancellationToken = default);
    Task AppendMessageAsync(ChatMessage message, CancellationToken cancellationToken = default);
    Task UpdateMessageContentAsync(string messageId, string content, CancellationToken cancellationToken = default);
    Task ClearMessagesAsync(string sessionId, CancellationToken cancellationToken = default);
}
