using DataChat.Core.Abstractions;
using DataChat.Core.Chat;
using DataChat.Core.Configuration;

namespace DataChat.Gateway.Services;

/// <summary>开发/演示：无真实领域服务时返回模拟流式回复。</summary>
public sealed class MockChatProvider : IChatProvider
{
    public string ProviderId => "mock";

    public bool CanHandle(DomainProfile domain) => true;

    public async IAsyncEnumerable<ChatChunk> StreamChatAsync(
        ChatContext context,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var reply = $"【演示模式】已收到您关于「{context.Domain.DisplayName}」的问题：\n{context.UserMessage}\n\n" +
                    "正式环境将由网关转发至公司自研领域服务或 DB-GPT。请在 appsettings 关闭 Gateway:UseMock 并配置 ApiKeys。";
        foreach (var ch in reply)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new ChatChunk { TextDelta = ch.ToString() };
            await Task.Delay(12, cancellationToken);
        }
        yield return new ChatChunk { IsCompleted = true };
    }
}
