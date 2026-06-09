// Copyright © 2023-present https://github.com/dymproject/purest-admin作者以及贡献者

using AIAdmin.Multiplex.AI.AIPlugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace AIAdmin.Multiplex.AI;

public static class AIExtensions
{
    public static IServiceCollection AddAIService(this IServiceCollection services, IConfiguration configuration)
    {
        var aiOptions = configuration.GetSection("AIOptions").Get<AIOptions>();
        var configured = aiOptions is not null
            && !string.IsNullOrWhiteSpace(aiOptions.Endpoint)
            && !string.IsNullOrWhiteSpace(aiOptions.ModelName)
            && !string.IsNullOrWhiteSpace(aiOptions.ApiKey);

        if (configured)
        {
            //services.AddAntiforgery();如果使用 Cookie 存储用户信息，必须开启 Antiforgery。并配合[ValidateAntiForgeryToken]使用。
            services.AddKernel()
                .AddOpenAIChatCompletion(modelId: aiOptions!.ModelName, apiKey: aiOptions.ApiKey, endpoint: new Uri(aiOptions.Endpoint))
                .Plugins.AddFromType<SystemPlugin>();
        }
        else
        {
            // 未配置 AI 时仍注册空 Kernel，避免 AuthService 等依赖无法解析；AI 对话接口会返回明确错误
            services.AddSingleton(Kernel.CreateBuilder().Build());
        }

        return services;
    }
}
