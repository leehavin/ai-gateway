using DataChat.Core.Chat;
using DataChat.Core.Configuration;

namespace DataChat.Core.Abstractions;

public interface IChatProvider
{
    string ProviderId { get; }
    bool CanHandle(DomainProfile domain);
    IAsyncEnumerable<ChatChunk> StreamChatAsync(ChatContext context, CancellationToken cancellationToken);
}
