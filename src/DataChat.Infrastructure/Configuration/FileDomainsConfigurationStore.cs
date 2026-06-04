using DataChat.Core.Abstractions;
using DataChat.Core.Configuration;

namespace DataChat.Infrastructure.Configuration;

public sealed class FileDomainsConfigurationStore : IDomainsConfigurationStore
{
    private readonly string _path;

    public FileDomainsConfigurationStore(string domainsFilePath) => _path = domainsFilePath;

    public Task<DomainsConfiguration> LoadAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(DomainsConfigurationLoader.Load(_path));
}
