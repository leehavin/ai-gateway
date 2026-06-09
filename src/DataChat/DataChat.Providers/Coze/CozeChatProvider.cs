using System.Runtime.CompilerServices;
using CozeNet.Chat;
using CozeNet.Chat.Models;
using CozeNet.Conversation;
using CozeNet.Core;
using CozeNet.Message.Models;
using DataChat.Core.Abstractions;
using DataChat.Core.Chat;
using DataChat.Core.Configuration;
using DataChat.Core.History;
using Microsoft.Extensions.Logging;

namespace DataChat.Providers.Coze;

/// <summary>
/// 扣子（Coze）智能体 Provider，基于 CozeNet SDK（NuGet）。
/// </summary>
public sealed class CozeChatProvider : IChatProvider
{
    private readonly CozeClientFactory _clientFactory;
    private readonly ICozeConversationStore _conversationStore;
    private readonly IDomainCatalog _domains;
    private readonly ILogger<CozeChatProvider> _logger;

    public CozeChatProvider(
        CozeClientFactory clientFactory,
        ICozeConversationStore conversationStore,
        IDomainCatalog domains,
        ILogger<CozeChatProvider> logger)
    {
        _clientFactory = clientFactory;
        _conversationStore = conversationStore;
        _domains = domains;
        _logger = logger;
    }

    public string ProviderId => "coze";

    public bool CanHandle(DomainProfile domain) =>
        domain.Provider.Equals("coze", StringComparison.OrdinalIgnoreCase);

    public async IAsyncEnumerable<ChatChunk> StreamChatAsync(
        ChatContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var coze = context.Domain.Coze;
        if (coze is null || string.IsNullOrWhiteSpace(coze.BotId))
        {
            yield return Error("领域未配置 Coze BotId。");
            yield break;
        }

        Context clientContext;
        string? authError = null;
        try
        {
            clientContext = await _clientFactory.CreateContextAsync(
                coze, _domains.Current.Defaults, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Coze 鉴权失败 domain={Domain}", context.Domain.Id);
            authError = ex.Message;
            clientContext = null!;
        }

        if (authError is not null)
        {
            yield return Error(authError);
            yield break;
        }

        var conversationId = await ResolveConversationIdAsync(clientContext, context, cancellationToken);
        if (conversationId.StartsWith("error:", StringComparison.Ordinal))
        {
            yield return Error(conversationId["error:".Length..]);
            yield break;
        }

        var maxTurns = _domains.Current.Defaults.MaxHistoryTurns > 0
            ? _domains.Current.Defaults.MaxHistoryTurns
            : 20;
        var history = ChatHistoryBuilder.Build(context.Domain, context.History, context.UserMessage, maxTurns);

        var request = new ChatRequest
        {
            BotID = coze.BotId,
            UserID = BuildCozeUserId(coze, context.Session.Id),
            Stream = true,
            AutoSaveHistory = coze.AutoSaveHistory,
            AdditionalMessages = coze.AutoSaveHistory
                ? [CozeMessageBuilder.BuildUserQuestion(context.UserMessage)]
                : CozeMessageBuilder.BuildAdditionalMessages(history),
            CustomVariables = coze.CustomVariables
        };

        _logger.LogInformation(
            "Coze chat domain={Domain} bot={BotId} session={SessionId} conversation={ConversationId}",
            context.Domain.Id, coze.BotId, context.Session.Id, conversationId);

        var chatService = new ChatService(clientContext);
        string? sendError = null;
        IAsyncEnumerable<StreamMessage>? stream = null;
        try
        {
            stream = chatService.SendStreamAsync(conversationId, request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Coze SendStreamAsync 失败");
            sendError = $"Coze 请求失败：{Truncate(ex.Message, 300)}";
        }

        if (sendError is not null)
        {
            yield return Error(sendError);
            yield break;
        }

        await foreach (var chunk in ReadStreamAsync(stream!, cancellationToken))
            yield return chunk;
    }

    private async Task<string> ResolveConversationIdAsync(
        Context clientContext,
        ChatContext context,
        CancellationToken cancellationToken)
    {
        var existing = await _conversationStore.GetConversationIdAsync(
            context.Domain.Id, context.Session.Id, cancellationToken);
        if (!string.IsNullOrWhiteSpace(existing))
            return existing;

        try
        {
            var conversationService = new ConversationService(clientContext);
            var created = await conversationService.CreateAsync(cancellationToken: cancellationToken);
            var conversationId = created?.Data?.ID;
            if (string.IsNullOrWhiteSpace(conversationId))
                return "error:Coze 创建会话失败：未返回 conversation_id。";

            await _conversationStore.SetConversationIdAsync(
                context.Domain.Id, context.Session.Id, conversationId, cancellationToken);
            return conversationId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Coze 创建会话失败");
            return $"error:Coze 创建会话失败：{Truncate(ex.Message, 300)}";
        }
    }

    private static async IAsyncEnumerable<ChatChunk> ReadStreamAsync(
        IAsyncEnumerable<StreamMessage> stream,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var hasDelta = false;
        await foreach (var update in stream.WithCancellation(cancellationToken))
        {
            if (update.Event is StreamEvents.DeltaMessage)
            {
                var message = update.Data as MessageObject;
                if (message?.Type is MessageType.Answer or MessageType.FollowUp)
                {
                    if (!string.IsNullOrEmpty(message.Content))
                    {
                        hasDelta = true;
                        yield return new ChatChunk { TextDelta = message.Content };
                    }
                }
            }
            else if (update.Event is StreamEvents.MessageComplete)
            {
                var message = update.Data as MessageObject;
                if (!hasDelta && message?.Type is MessageType.Answer or MessageType.FollowUp)
                {
                    if (!string.IsNullOrEmpty(message.Content))
                        yield return new ChatChunk { TextDelta = message.Content };
                }
            }
            else if (update.Event is StreamEvents.ChatFailed)
            {
                var chat = update.Data as ChatObject;
                yield return Error(FormatCozeError(chat?.LastError) ?? "Coze 对话失败。");
                yield break;
            }
            else if (update.Event is StreamEvents.Error)
            {
                yield return Error(update.Data?.ToString() ?? "Coze 流式错误。");
                yield break;
            }
            else if (update.Event is StreamEvents.RequireAction)
            {
                yield return Error("Coze Bot 需要工具回调（RequireAction），当前 Gateway 未启用工具执行。");
                yield break;
            }
            else if (update.Event is StreamEvents.ChatComplete or StreamEvents.Done)
            {
                yield return new ChatChunk { IsCompleted = true };
                yield break;
            }
        }

        yield return new ChatChunk { IsCompleted = true };
    }

    private static string? FormatCozeError(object? lastError) =>
        lastError switch
        {
            null => null,
            _ when lastError.GetType().GetProperty("Msg")?.GetValue(lastError) is string msg && !string.IsNullOrWhiteSpace(msg) => msg,
            _ => lastError.ToString()
        };

    private static string BuildCozeUserId(CozeDomainOptions coze, string sessionId)
    {
        var prefix = string.IsNullOrWhiteSpace(coze.UserIdPrefix) ? "datachat" : coze.UserIdPrefix.Trim();
        return $"{prefix}:{sessionId}";
    }

    private static ChatChunk Error(string message) => new() { Error = message, IsCompleted = true };

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "...";
}
