namespace DataChat.Core.Abstractions;

public interface IApiKeyStore
{
    Task<string?> GetAsync(string keyRef, CancellationToken cancellationToken = default);
    Task SetAsync(string keyRef, string apiKey, CancellationToken cancellationToken = default);
}
