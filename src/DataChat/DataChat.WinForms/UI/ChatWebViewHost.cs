using System.Linq;
using System.Text.Json;
using DataChat.Core.Entities;
using Microsoft.Web.WebView2.WinForms;

namespace DataChat.WinForms.UI;

public sealed class ChatWebViewHost : Panel
{
    private readonly WebView2 _webView = new() { Dock = DockStyle.Fill };

    public ChatWebViewHost()
    {
        Controls.Add(_webView);
    }

    public async Task InitializeAsync(string wwwrootPath)
    {
        await _webView.EnsureCoreWebView2Async();
        var htmlPath = Path.Combine(wwwrootPath, "chat.html");
        _webView.CoreWebView2.Navigate(new Uri(htmlPath).AbsoluteUri);
    }

    public async Task RenderMessagesAsync(IReadOnlyList<ChatMessage> messages)
    {
        await EnsureReadyAsync();
        var payload = JsonSerializer.Serialize(messages.Select(m => new
        {
            m.Id,
            m.Role,
            m.Content
        }));
        await ExecAsync($"window.chatApi.renderMessages({payload})");
    }

    public Task AppendMessageAsync(string id, string role, string content, bool streaming) =>
        ExecAsync($"window.chatApi.appendMessage({Js(id)},{Js(role)},{Js(content)},{streaming.ToString().ToLowerInvariant()})");

    public Task UpdateMessageAsync(string id, string content, bool streaming) =>
        ExecAsync($"window.chatApi.updateMessage({Js(id)},{Js(content)},{streaming.ToString().ToLowerInvariant()})");

    private async Task EnsureReadyAsync()
    {
        if (_webView.CoreWebView2 is null)
            await _webView.EnsureCoreWebView2Async();
    }

    private async Task ExecAsync(string script)
    {
        await EnsureReadyAsync();
        await _webView.CoreWebView2.ExecuteScriptAsync(script);
    }

    /// <summary>向嵌入的 chat-ui 注入用户身份（WinForms / IPSpace 宿主登录后调用）。</summary>
    public async Task InjectUserAsync(string userId, string userName, string token)
    {
        await EnsureReadyAsync();
        var payload = JsonSerializer.Serialize(new
        {
            type = "datachat:setUser",
            userId,
            userName,
            token
        });
        await ExecAsync($"window.postMessage({payload}, '*')");
    }

    /// <summary>
    /// 触发嵌入 chat-ui 执行 Coze 工作流（案件五书核稿等）。
    /// files 的 fileId 来自 Gateway <c>/v1/files/upload</c> 或 <c>/v1/files/register</c>。
    /// </summary>
    public async Task RunWorkflowAsync(
        string domainId,
        string workflowId,
        string? input,
        IReadOnlyList<(string FileId, string Name)>? files = null,
        bool newSession = true)
    {
        await EnsureReadyAsync();
        var payload = JsonSerializer.Serialize(new
        {
            type = "datachat:runWorkflow",
            domainId,
            workflowId,
            input,
            files = files?.Select(f => new { fileId = f.FileId, name = f.Name }).ToArray(),
            newSession
        });
        await ExecAsync($"window.postMessage({payload}, '*')");
    }

    private static string Js(string s) => JsonSerializer.Serialize(s);
}
