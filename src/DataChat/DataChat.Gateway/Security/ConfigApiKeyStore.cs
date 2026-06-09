using DataChat.Core.Abstractions;

namespace DataChat.Gateway.Security;

public sealed class ConfigApiKeyStore : IApiKeyStore
{
    private readonly IConfiguration _configuration;

    public ConfigApiKeyStore(IConfiguration configuration) => _configuration = configuration;

    public Task<string?> GetAsync(string keyRef, CancellationToken cancellationToken = default)
    {
        var value = _configuration[$"ApiKeys:{keyRef}"];
        return Task.FromResult(string.IsNullOrWhiteSpace(value) ? null : value);
    }

    public Task SetAsync(string keyRef, string apiKey, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("网关密钥请通过配置或密钥管理服务注入。");
}
