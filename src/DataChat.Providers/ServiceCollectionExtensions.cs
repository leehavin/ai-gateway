using DataChat.Core.Abstractions;

using DataChat.Core.Chat;

using DataChat.Core.Configuration;

using DataChat.Providers.Coze;

using DataChat.Providers.Custom;

using DataChat.Providers.Dbgpt;

using Microsoft.Extensions.DependencyInjection;



namespace DataChat.Providers;



public static class ServiceCollectionExtensions

{

    public static IServiceCollection AddDataChatProviders(

        this IServiceCollection services,

        DomainsConfiguration domains)
    {
        var timeoutSeconds = Math.Max(30, domains.Defaults.TimeoutSeconds);

        services.AddHttpClient("DataChat", client =>

        {

            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

        });

        services.AddHttpClient(CozeHttpClientNames.Client, client =>

        {

            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

        });



        services.AddSingleton<CozeClientFactory>();

        services.AddSingleton<ICozeConversationStore, InMemoryCozeConversationStore>();

        services.AddSingleton<DbgptApiClient>(sp => new DbgptApiClient(

            sp.GetRequiredService<IHttpClientFactory>(),

            sp.GetRequiredService<IApiKeyStore>(),

            domains.Defaults.DbgptBaseUrl));



        services.AddSingleton<IChatProvider, CustomDomainProvider>();

        services.AddSingleton<IChatProvider, DbGptChatProvider>(sp => new DbGptChatProvider(

            sp.GetRequiredService<IHttpClientFactory>(),

            sp.GetRequiredService<IApiKeyStore>(),

            domains.Defaults.DbgptBaseUrl));

        services.AddSingleton<IChatProvider, CozeChatProvider>();

        services.AddSingleton<ChatOrchestrator>();

        return services;

    }



    /// <summary>兼容旧调用：仅传入 DB-GPT 地址时仍可用。</summary>

    public static IServiceCollection AddDataChatProviders(this IServiceCollection services, string dbgptBaseUrl)

    {

        var domains = new DomainsConfiguration

        {

            Defaults = new GlobalDefaults { DbgptBaseUrl = dbgptBaseUrl }

        };

        return services.AddDataChatProviders(domains);

    }

}

