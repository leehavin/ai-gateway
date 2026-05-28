using CozeNet.Conversation;
using CozeNet.Core;
using DataChat.Core.Configuration;
using DataChat.Providers.Coze;

namespace DataChat.Gateway.Services;

/// <summary>Coze 资源探测与健康检查。</summary>
public sealed class CozeResourceService
{
    private readonly CozeClientFactory _clientFactory;
    private readonly DomainsConfiguration _domains;

    public CozeResourceService(CozeClientFactory clientFactory, DomainsConfiguration domains)
    {
        _clientFactory = clientFactory;
        _domains = domains;
    }

    public IReadOnlyList<CozeBotSummary> ListConfiguredBots()
    {
        return _domains.Domains
            .Where(d => d.Provider.Equals("coze", StringComparison.OrdinalIgnoreCase) && d.Coze is not null)
            .Select(d => new CozeBotSummary
            {
                DomainId = d.Id,
                DisplayName = d.DisplayName,
                BotId = d.Coze!.BotId,
                Endpoint = d.Coze.Endpoint ?? _domains.Defaults.CozeEndpoint,
                ApiKeyRef = d.Coze.ApiKeyRef
            })
            .ToList();
    }

    public async Task<bool> PingAsync(CancellationToken cancellationToken = default)
    {
        var bot = ListConfiguredBots().FirstOrDefault();
        if (bot is null)
            return false;

        try
        {
            var context = await _clientFactory.CreateContextAsync(bot.Endpoint, bot.ApiKeyRef, cancellationToken);
            var conversationService = new ConversationService(context);
            var created = await conversationService.CreateAsync(cancellationToken: cancellationToken);
            return !string.IsNullOrWhiteSpace(created?.Data?.ID);
        }
        catch
        {
            return false;
        }
    }
}

public sealed class CozeBotSummary
{
    public required string DomainId { get; init; }
    public required string DisplayName { get; init; }
    public required string BotId { get; init; }
    public required string Endpoint { get; init; }
    public required string ApiKeyRef { get; init; }
}
