using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DataChat.Core.Abstractions;

namespace DataChat.Providers.Dbgpt;

/// <summary>DB-GPT 5670 应用层 API 客户端（对齐官方 /api/v2）。</summary>
public sealed class DbgptApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IApiKeyStore _apiKeyStore;
    private readonly string _baseUrl;
    public DbgptApiClient(
        IHttpClientFactory httpClientFactory,
        IApiKeyStore apiKeyStore,
        string baseUrl)
    {
        _httpClientFactory = httpClientFactory;
        _apiKeyStore = apiKeyStore;
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public string BaseUrl => _baseUrl;

    public Task<HttpResponseMessage> GetAsync(string relativePath, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Get, relativePath, null, cancellationToken);

    public Task<HttpResponseMessage> PostAsync(string relativePath, object? body, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, relativePath, body, cancellationToken);

    /// <summary>流式读取响应体（必须在读 Body 前调用，否则会缓冲整段响应）。</summary>
    public Task<HttpResponseMessage> PostStreamingAsync(string relativePath, object? body, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, relativePath, body, cancellationToken, streaming: true);

    public Task<HttpResponseMessage> PutAsync(string relativePath, object? body, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Put, relativePath, body, cancellationToken);

    public Task<HttpResponseMessage> DeleteAsync(string relativePath, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Delete, relativePath, null, cancellationToken);

    public async Task<bool> PingAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await GetAsync("/docs", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string relativePath,
        object? body,
        CancellationToken cancellationToken,
        bool streaming = false)
    {
        var client = _httpClientFactory.CreateClient("DataChat");
        var apiKey = await _apiKeyStore.GetAsync("dbgpt-main", cancellationToken);
        if (!string.IsNullOrEmpty(apiKey))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var path = relativePath.StartsWith('/') ? relativePath : "/" + relativePath;
        using var request = new HttpRequestMessage(method, _baseUrl + path);
        if (body is not null)
        {
            request.Content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json");
        }

        return await client.SendAsync(
            request,
            streaming ? HttpCompletionOption.ResponseHeadersRead : HttpCompletionOption.ResponseContentRead,
            cancellationToken);
    }
}
