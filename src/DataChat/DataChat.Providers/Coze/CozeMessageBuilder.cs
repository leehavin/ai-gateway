using CozeNet.Message.Models;
using DataChat.Core.History;

namespace DataChat.Providers.Coze;

internal static class CozeMessageBuilder
{
    public static List<BaseMessageObject> BuildAdditionalMessages(IReadOnlyList<HistoryMessage> history)
    {
        var messages = new List<BaseMessageObject>();
        foreach (var item in history)
        {
            if (item.Role is not ("user" or "assistant"))
                continue;

            messages.Add(new BaseMessageObject
            {
                Role = item.Role == "assistant" ? RoleType.Assistant : RoleType.User,
                Type = item.Role == "assistant" ? MessageType.Answer : MessageType.Question,
                Content = item.Content,
                ContentType = ContentType.Text
            });
        }

        return messages;
    }

    public static BaseMessageObject BuildUserQuestion(string userMessage) => new()
    {
        Role = RoleType.User,
        Type = MessageType.Question,
        Content = userMessage.Trim(),
        ContentType = ContentType.Text
    };
}
