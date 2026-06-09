using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AIAdmin.Application.Gateway;

public interface IDataChatGatewayNotifier
{
    /// <summary>后台触发 Gateway 重载，不阻塞当前请求。</summary>
    void NotifyReloadDomains();

    Task TryReloadDomainsAsync(CancellationToken cancellationToken = default);
}

public class DataChatGatewayNotifier(
    IHttpClientFactory httpClientFactory,
    IOptions<DataChatGatewayOptions> options,
    ILogger<DataChatGatewayNotifier> logger) : IDataChatGatewayNotifier, ISingletonDependency
{
    public void NotifyReloadDomains() => _ = TryReloadDomainsAsync();

    public async Task TryReloadDomainsAsync(CancellationToken cancellationToken = default)
    {
        var cfg = options.Value;
        if (!cfg.AutoReload || cfg.BaseUrl.IsNullOrWhiteSpace())
            return;

        var url = $"{cfg.BaseUrl!.TrimEnd('/')}/v1/domains/reload";
        try
        {
            using var client = httpClientFactory.CreateClient(nameof(DataChatGatewayNotifier));
            using var response = await client.PostAsync(url, null, cancellationToken);
            if (!response.IsSuccessStatusCode)
                logger.LogWarning("Gateway 领域配置重载失败: {Status} {Url}", response.StatusCode, url);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            logger.LogWarning("Gateway 未响应，跳过领域配置重载: {Url}", url);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Gateway 领域配置重载异常: {Url}", url);
        }
    }
}
