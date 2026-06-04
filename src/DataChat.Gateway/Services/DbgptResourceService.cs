using DataChat.Core.Abstractions;
using DataChat.Core.Configuration;
using DataChat.Providers.Dbgpt;

namespace DataChat.Gateway.Services;

/// <summary>DB-GPT 连通性探测（供健康检查）。对话走 <see cref="DbGptChatProvider"/>。</summary>
public sealed class DbgptResourceService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IApiKeyStore _apiKeyStore;
    private readonly IDomainCatalog _catalog;

    public DbgptResourceService(
        IHttpClientFactory httpClientFactory,
        IApiKeyStore apiKeyStore,
        IDomainCatalog catalog)
    {
        _httpClientFactory = httpClientFactory;
        _apiKeyStore = apiKeyStore;
        _catalog = catalog;
    }

    public Task<bool> PingAsync(CancellationToken cancellationToken = default) =>
        new DbgptApiClient(_httpClientFactory, _apiKeyStore, _catalog.Current.Defaults.DbgptBaseUrl)
            .PingAsync(cancellationToken);
}
