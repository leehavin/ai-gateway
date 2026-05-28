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

    private static string Js(string s) => JsonSerializer.Serialize(s);
}
