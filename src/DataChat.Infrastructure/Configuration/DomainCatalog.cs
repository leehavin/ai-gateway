using DataChat.Core.Abstractions;
using DataChat.Core.Configuration;

namespace DataChat.Infrastructure.Configuration;

public sealed class DomainCatalog : IDomainCatalog
{
    private readonly IDomainsConfigurationStore _store;
    private DomainsConfiguration _current;

    public DomainCatalog(IDomainsConfigurationStore store, DomainsConfiguration initial)
    {
        _store = store;
        _current = initial;
    }

    public DomainsConfiguration Current => _current;

    public async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        _current = await _store.LoadAsync(cancellationToken);
    }
}
