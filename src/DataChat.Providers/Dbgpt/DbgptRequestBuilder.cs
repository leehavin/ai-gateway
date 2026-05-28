using System.Text;
using DataChat.Core.History;

namespace DataChat.Providers.Dbgpt;

/// <summary>
/// 按官方 dbgpt_client 约定组装请求：messages 为 string | string[]，非 OpenAI message 数组。
/// </summary>
public static class DbgptRequestBuilder
{
    public static object BuildMessages(IReadOnlyList<HistoryMessage> history, string userMessage)
    {
        var turns = history
            .Where(m => m.Role is "user" or "assistant")
            .Select(m => m.Role == "user" ? $"User: {m.Content}" : $"Assistant: {m.Content}")
            .ToList();

        var system = history.FirstOrDefault(m => m.Role == "system")?.Content;
        if (!string.IsNullOrWhiteSpace(system))
            turns.Insert(0, $"System: {system}");

        turns.Add($"User: {userMessage.Trim()}");

        if (turns.Count == 1)
            return userMessage.Trim();

        return string.Join("\n", turns);
    }

    public static Dictionary<string, object?> BuildCompletionBody(
        string model,
        IReadOnlyList<HistoryMessage> history,
        string userMessage,
        string chatMode,
        string? chatParam,
        bool stream)
    {
        return new Dictionary<string, object?>
        {
            ["model"] = model,
            ["messages"] = BuildMessages(history, userMessage),
            ["chat_mode"] = chatMode,
            ["chat_param"] = chatParam ?? "",
            ["stream"] = stream,
            ["incremental"] = true,
            ["enable_vis"] = true
        };
    }
}
