using DataChat.Application;
using DataChat.Core.Abstractions;
using DataChat.Core.Configuration;
using DataChat.Infrastructure.Configuration;
using DataChat.Infrastructure.Persistence;
using DataChat.Infrastructure.Security;
using DataChat.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DataChat.WinForms;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        var host = BuildHost();
        SeedDemoApiKeysIfMissing(host.Services);
        // IHostedService（含 DatabaseInitializer）只有在 StartAsync 后才会执行
        host.StartAsync().GetAwaiter().GetResult();
        try
        {
            System.Windows.Forms.Application.Run(host.Services.GetRequiredService<MainForm>());
        }
        finally
        {
            host.StopAsync().GetAwaiter().GetResult();
        }
    }

    private static IHost BuildHost()
    {
        var baseDir = AppContext.BaseDirectory;
        var domainsPath = Path.Combine(baseDir, "domains.json");
        var domains = DomainsConfigurationLoader.Load(domainsPath);
        var dbgptUrl = domains.Defaults.DbgptBaseUrl;

        return Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(domains);
                services.AddSingleton<IApiKeyStore, FileApiKeyStore>();
                services.AddSingleton<IConversationRepository>(sp =>
                {
                    var dbPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "DataChat", "chat.db");
                    return new SqliteConversationRepository(dbPath);
                });
                services.AddDataChatProviders(domains);
                services.AddSingleton<ChatService>();
                services.AddSingleton<MainForm>();
                services.AddHostedService<DatabaseInitializer>();
            })
            .Build();
    }

    /// <summary>本地联调：写入与 Gateway ValidTokens 一致的演示密钥（仅当尚未配置时）。</summary>
    private static void SeedDemoApiKeysIfMissing(IServiceProvider services)
    {
        var store = services.GetRequiredService<IApiKeyStore>();
        SeedKey(store, "gateway-token", "demo-token");
        SeedKey(store, "dbgpt-main", "dbgpt");
        SeedKey(store, "coze-main", "");
    }

    private static void SeedKey(IApiKeyStore store, string keyRef, string value)
    {
        if (store.GetAsync(keyRef).GetAwaiter().GetResult() is null)
            store.SetAsync(keyRef, value).GetAwaiter().GetResult();
    }
}

internal sealed class DatabaseInitializer : IHostedService
{
    private readonly IConversationRepository _repository;

    public DatabaseInitializer(IConversationRepository repository) => _repository = repository;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_repository is SqliteConversationRepository sqlite)
            await sqlite.InitializeAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
