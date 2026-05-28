using DataChat.Core.Configuration;
using DataChat.Core.Entities;

namespace DataChat.Core.History;

/// <summary>
/// 已确认：仅本地拼 history，不依赖服务端 conv_uid。
/// </summary>
public static class ChatHistoryBuilder
{
    public static IReadOnlyList<HistoryMessage> Build(
        DomainProfile domain,
        IReadOnlyList<ChatMessage> storedMessages,
        string userMessage,
        int maxTurns)
    {
        var list = new List<HistoryMessage>();
        if (!string.IsNullOrWhiteSpace(domain.SystemPrompt))
            list.Add(new HistoryMessage("system", domain.SystemPrompt));

        var recent = storedMessages
            .Where(m => m.Role is "user" or "assistant")
            .OrderBy(m => m.CreatedAt)
            .TakeLast(maxTurns * 2)
            .Select(m => new HistoryMessage(m.Role, m.Content));

        list.AddRange(recent);
        list.Add(new HistoryMessage("user", userMessage));
        return list;
    }
}

public sealed record HistoryMessage(string Role, string Content);
