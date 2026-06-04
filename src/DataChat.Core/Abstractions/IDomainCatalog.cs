using DataChat.Core.Configuration;

namespace DataChat.Core.Abstractions;

/// <summary>运行时领域目录（支持从数据库重载）。</summary>
public interface IDomainCatalog
{
    DomainsConfiguration Current { get; }

    Task ReloadAsync(CancellationToken cancellationToken = default);
}
