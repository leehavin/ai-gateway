using DataChat.Core.Abstractions;
using DataChat.Core.Chat;
using DataChat.Core.Configuration;
using DataChat.Gateway.Auth;
using DataChat.Gateway.Configuration;
using DataChat.Gateway.Middleware;
using DataChat.Gateway.Security;
using DataChat.Gateway.Services;
using DataChat.Infrastructure.Configuration;
using DataChat.Infrastructure.Persistence;
using DataChat.Providers;
using DataChat.Providers.Coze;
using Microsoft.Extensions.Options;

namespace DataChat.Gateway.Extensions;

public static class GatewayServiceExtensions
{
    public static IServiceCollection AddDataChatGateway(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<GatewayOptions>(configuration.GetSection(GatewayOptions.SectionName));
        services.AddHttpContextAccessor();
        services.AddSingleton<SessionTokenService>();
        services.AddSingleton<GatewayAuthService>();
        services.AddSingleton<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IHostAuthProvider, LocalConfigAuthProvider>();

        var gatewayOptions = configuration.GetSection(GatewayOptions.SectionName).Get<GatewayOptions>()
            ?? new GatewayOptions();

        var dbOptions = DataChatDatabaseExtensions.ReadDbOptions(configuration);
        var domainsSetup = new DomainsSetupOptions
        {
            Source = DomainsSourceParser.Parse(gatewayOptions.DomainsSource),
            DomainsFile = gatewayOptions.DomainsFile,
            DatabasePath = gatewayOptions.DatabasePath,
            SeedFromFileWhenEmpty = gatewayOptions.SeedDomainsFromFileWhenEmpty
        };

        services.AddSingleton<IApiKeyStore, ConfigApiKeyStore>();

        var baseDir = AppContext.BaseDirectory;
        var sqlSugar = DataChatDatabaseExtensions.AddDataChatDatabase(services, dbOptions, domainsSetup, baseDir);
        var domains = DomainsConfigurationRegistrar.RegisterDomains(services, domainsSetup, baseDir, sqlSugar);
        services.AddSingleton<FileStorageService>();
        services.AddSingleton<GatewaySessionService>();
        services.AddSingleton<FeedbackService>();
        services.AddSingleton<GatewayChatService>();
        services.AddSingleton<DbgptResourceService>();
        services.AddSingleton<CozeResourceService>();
        services.AddSingleton<CozeWorkflowService>();
        services.AddSingleton<CozeClientFactory>();
        services.AddSingleton<CozeOpenApiClient>();

        var timeoutSeconds = gatewayOptions.TimeoutSeconds > 0
            ? gatewayOptions.TimeoutSeconds
            : domains.Defaults.TimeoutSeconds;

        services.AddHttpClient("DataChat", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(Math.Max(30, timeoutSeconds));
        });

        if (gatewayOptions.UseMock)
        {
            services.AddSingleton<IChatProvider, MockChatProvider>();
            services.AddSingleton<ChatOrchestrator>();
        }
        else
        {
            services.AddDataChatProviders(domains);
        }

        return services;
    }

    public static WebApplication UseDataChatGateway(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<GatewayOptions>>().Value;
        if (!string.IsNullOrWhiteSpace(options.PathBase))
            app.UsePathBase(options.PathBase);

        app.UseMiddleware<GatewayExceptionMiddleware>();
        app.UseMiddleware<GatewayAuthMiddleware>();

        return app;
    }
}
