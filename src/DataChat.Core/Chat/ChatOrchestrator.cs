using DataChat.Core.Abstractions;
using DataChat.Core.Configuration;

namespace DataChat.Core.Chat;

public sealed class ChatOrchestrator
{
    private readonly IEnumerable<IChatProvider> _providers;

    public ChatOrchestrator(IEnumerable<IChatProvider> providers) => _providers = providers;

    public IAsyncEnumerable<ChatChunk> SendAsync(ChatContext context, CancellationToken cancellationToken)
    {
        var provider = _providers.FirstOrDefault(p => p.CanHandle(context.Domain))
            ?? throw new InvalidOperationException($"未找到领域 {context.Domain.Id} 的 Provider（{context.Domain.Provider}）。");
        return provider.StreamChatAsync(context, cancellationToken);
    }
}
