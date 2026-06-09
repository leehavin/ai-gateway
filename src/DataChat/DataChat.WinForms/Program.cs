using DataChat.Core.Abstractions;
using DataChat.Core.Configuration;
using DataChat.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
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

        return Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(cfg =>
            {
                cfg.AddJsonFile("appsettings.Development.local.json", optional: true, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                var config = context.Configuration;
                var dbOptions = DataChatDatabaseExtensions.ReadDbOptions(config);
                var domainsSetup = new DomainsSetupOptions
                {
                    Source = DomainsSourceParser.Parse(config["DomainsSource"] ?? "File"),
                    DomainsFile = config["DomainsFile"] ?? "domains.json",
                    DatabasePath = config["DatabasePath"] ?? dbOptions.DatabasePath,
                    SeedFromFileWhenEmpty = !string.Equals(
                        config["SeedDomainsFromFileWhenEmpty"],
                        "false",
                        StringComparison.OrdinalIgnoreCase)
                };
                services.AddSingleton<IApiKeyStore, FileApiKeyStore>();
                var sqlSugar = DataChatDatabaseExtensions.AddDataChatDatabase(
                    services,
                    dbOptions,
                    domainsSetup,
                    baseDir);
                var domains = DomainsConfigurationRegistrar.RegisterDomains(
                    services,
                    domainsSetup,
                    baseDir,
                    sqlSugar);
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
        if (_repository is IDatabaseInitializer init)
            await init.InitializeAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
