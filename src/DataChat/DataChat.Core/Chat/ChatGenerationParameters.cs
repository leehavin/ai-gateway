namespace DataChat.Core.Chat;

/// <summary>可选生成参数，由 Gateway 转发至支持的 Provider。</summary>
public sealed class ChatGenerationParameters
{
    public double? Temperature { get; init; }
    public double? TopP { get; init; }
    public int? MaxTokens { get; init; }
}
