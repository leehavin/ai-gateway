namespace DataChat.Gateway.Models;

/// <summary>宿主用 ServiceKey 将本地已有文件登记到 Gateway 上传目录（供工作流 doc 等参数使用）。</summary>
public sealed class RegisterFileRequest
{
    public string Path { get; set; } = "";
    public string? ServiceKey { get; set; }
}
