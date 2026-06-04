using DataChat.Core.Configuration;

namespace DataChat.Core.Abstractions;

/// <summary>领域配置数据源（文件 / 数据库等）。</summary>
public interface IDomainsConfigurationStore
{
    Task<DomainsConfiguration> LoadAsync(CancellationToken cancellationToken = default);
}
