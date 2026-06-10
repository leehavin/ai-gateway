using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DataChat.WinForms.UI;

namespace DataChat.WinForms;

/// <summary>
/// WinForms / IPSpace 宿主与嵌入 chat-ui 的集成辅助：登记案件文档并触发 Coze 工作流。
/// </summary>
public sealed class HostChatBridge : IDisposable
{
    private readonly HttpClient _http;
    private readonly ChatWebViewHost _webView;
    private readonly string _gatewayBaseUrl;
    private readonly string _serviceKey;

    public HostChatBridge(
        ChatWebViewHost webView,
        string gatewayBaseUrl,
        string serviceKey,
        HttpClient? httpClient = null)
    {
        _webView = webView;
        _gatewayBaseUrl = gatewayBaseUrl.TrimEnd('/');
        _serviceKey = serviceKey;
        _http = httpClient ?? new HttpClient();
    }

    /// <summary>用 ServiceKey 将本地文件登记到 Gateway，返回 fileId。</summary>
    public async Task<(string FileId, string Name)> RegisterLocalFileAsync(
        string localPath,
        CancellationToken cancellationToken = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{_gatewayBaseUrl}/v1/files/register");
        req.Headers.Add("X-Service-Key", _serviceKey);
        req.Content = JsonContent.Create(new { path = localPath });

        using var res = await _http.SendAsync(req, cancellationToken);
        var body = await res.Content.ReadAsStringAsync(cancellationToken);
        if (!res.IsSuccessStatusCode)
            throw new InvalidOperationException($"登记文件失败 {res.StatusCode}: {body}");

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var fileId = root.GetProperty("fileId").GetString()
            ?? throw new InvalidOperationException("登记文件响应缺少 fileId。");
        var name = root.GetProperty("name").GetString()
            ?? Path.GetFileName(localPath);
        return (fileId, name);
    }

    /// <summary>多份本地文档登记后，通知 chat-ui 执行工作流（如 PatentOA 五书核稿）。</summary>
    public async Task RunWorkflowFromLocalFilesAsync(
        string userToken,
        string domainId,
        string workflowId,
        IReadOnlyList<string> localPaths,
        string? input = null,
        bool newSession = true,
        CancellationToken cancellationToken = default)
    {
        if (localPaths.Count == 0)
            throw new InvalidOperationException("请至少提供一份文档。");

        var files = new List<(string FileId, string Name)>(localPaths.Count);
        foreach (var path in localPaths)
        {
            files.Add(await RegisterLocalFileAsync(path, cancellationToken));
        }

        await _webView.RunWorkflowAsync(
            domainId,
            workflowId,
            input ?? "请对本案五书进行核稿",
            files,
            newSession);
    }

    /// <summary>用用户 Token 通过 multipart 上传（无需 ServiceKey 读本地路径权限时可用）。</summary>
    public async Task<(string FileId, string Name)> UploadFileAsync(
        string userToken,
        string localPath,
        CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(localPath);
        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(fileContent, "file", Path.GetFileName(localPath));

        using var req = new HttpRequestMessage(HttpMethod.Post, $"{_gatewayBaseUrl}/v1/files/upload");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
        req.Content = content;

        using var res = await _http.SendAsync(req, cancellationToken);
        var body = await res.Content.ReadAsStringAsync(cancellationToken);
        if (!res.IsSuccessStatusCode)
            throw new InvalidOperationException($"上传文件失败 {res.StatusCode}: {body}");

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var fileId = root.GetProperty("fileId").GetString()
            ?? throw new InvalidOperationException("上传响应缺少 fileId。");
        var name = root.GetProperty("name").GetString()
            ?? Path.GetFileName(localPath);
        return (fileId, name);
    }

    public void Dispose() => _http.Dispose();
}
