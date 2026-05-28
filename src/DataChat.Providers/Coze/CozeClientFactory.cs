using CozeNet.Core;
using DataChat.Core.Abstractions;

namespace DataChat.Providers.Coze;

public sealed class CozeClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IApiKeyStore _apiKeyStore;

    public CozeClientFactory(IHttpClientFactory httpClientFactory, IApiKeyStore apiKeyStore)
    {
        _httpClientFactory = httpClientFactory;
        _apiKeyStore = apiKeyStore;
    }

    public async Task<Context> CreateContextAsync(
        string endpoint,
        string apiKeyRef,
        CancellationToken cancellationToken = default)
    {
        var token = await _apiKeyStore.GetAsync(apiKeyRef, cancellationToken);
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException($"未配置 Coze API 密钥：ApiKeys:{apiKeyRef}");

        return new Context
        {
            AccessToken = token.Trim(),
            EndPoint = NormalizeEndpoint(endpoint),
            HttpClient = _httpClientFactory.CreateClient(CozeHttpClientNames.Client)
        };
    }

    internal static string NormalizeEndpoint(string endpoint)
    {
        var value = endpoint.Trim();
        if (value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            value = value["https://".Length..];
        if (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            value = value["http://".Length..];
        return value.TrimEnd('/');
    }
}

public static class CozeHttpClientNames
{
    public const string Client = "CozeNet";
}
