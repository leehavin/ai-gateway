using DataChat.Core.Abstractions;
using DataChat.Core.Configuration;

namespace DataChat.Gateway.Services;

/// <summary>从领域配置汇总 Coze Bot 列表（供 chat-ui / 命令菜单）。</summary>
public sealed class CozeResourceService
{
    private readonly IDomainCatalog _domains;

    public CozeResourceService(IDomainCatalog domains) => _domains = domains;

    public IReadOnlyList<CozeBotSummary> ListConfiguredBots() =>
        _domains.Current.Domains
            .Where(d => d.Provider.Equals("coze", StringComparison.OrdinalIgnoreCase) && d.Coze is not null)
            .Select(d => new CozeBotSummary
            {
                DomainId = d.Id,
                DisplayName = d.DisplayName,
                BotId = d.Coze!.BotId,
                Endpoint = d.Coze.Endpoint ?? _domains.Current.Defaults.CozeEndpoint
            })
            .ToList();
}

public sealed class CozeBotSummary
{
    public required string DomainId { get; init; }
    public required string DisplayName { get; init; }
    public required string BotId { get; init; }
    public required string Endpoint { get; init; }
}
