using DataChat.Core.Configuration;

namespace DataChat.Infrastructure.Configuration;

public sealed class DomainsSetupOptions
{
    public DomainsSource Source { get; set; } = DomainsSource.File;
    public string DomainsFile { get; set; } = "domains.json";
    public string DatabasePath { get; set; } = "data/datachat.db";
    /// <summary>数据库模式且表为空时，从 DomainsFile 导入种子数据。</summary>
    public bool SeedFromFileWhenEmpty { get; set; } = true;
}
