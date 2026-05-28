using DataChat.Core.Abstractions;
using DataChat.Core.Chat;
using DataChat.Core.Configuration;
using DataChat.Gateway.Configuration;
using DataChat.Gateway.Middleware;
using DataChat.Gateway.Security;
using DataChat.Gateway.Services;
using DataChat.Infrastructure.Configuration;
using DataChat.Providers;
using Microsoft.Extensions.Options;

namespace DataChat.Gateway.Extensions;

public static class GatewayServiceExtensions
{
    public static IServiceCollection AddDataChatGateway(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<GatewayOptions>(configuration.GetSection(GatewayOptions.SectionName));

        var gatewayOptions = configuration.GetSection(GatewayOptions.SectionName).Get<GatewayOptions>()
            ?? new GatewayOptions();

        var domainsPath = Path.Combine(AppContext.BaseDirectory, gatewayOptions.DomainsFile);
        var domains = DomainsConfigurationLoader.Load(domainsPath);
        services.AddSingleton(domains);

        services.AddSingleton<IApiKeyStore, ConfigApiKeyStore>();
        services.AddSingleton<FileStorageService>();
        services.AddSingleton<GatewayChatService>();
        services.AddSingleton<DbgptResourceService>();
        services.AddSingleton<CozeResourceService>();

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
