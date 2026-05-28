using DataChat.Core.Configuration;
using DataChat.Core.Entities;

namespace DataChat.Core.Chat;

public sealed class ChatContext
{
    public required ChatSession Session { get; init; }
    public required DomainProfile Domain { get; init; }
    public required IReadOnlyList<ChatMessage> History { get; init; }
    public required string UserMessage { get; init; }
}
