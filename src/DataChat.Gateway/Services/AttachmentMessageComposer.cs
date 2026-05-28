using System.Text;
using DataChat.Gateway.Models;

namespace DataChat.Gateway.Services;

public static class AttachmentMessageComposer
{
    public static string Compose(string message, IReadOnlyList<ChatAttachmentDto>? attachments, FileStorageService files)
    {
        if (attachments is null || attachments.Count == 0)
            return message.Trim();

        var sb = new StringBuilder(message.Trim());
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine("【用户附件】");

        foreach (var att in attachments)
        {
            var stored = files.Get(att.FileId);
            var name = att.Name ?? stored?.Name ?? att.FileId;
            sb.AppendLine($"- {name} (fileId: {att.FileId})");

            if (stored is not null)
            {
                var preview = files.TryReadTextPreviewAsync(stored.Path, stored.Name, CancellationToken.None)
                    .GetAwaiter().GetResult();
                if (!string.IsNullOrWhiteSpace(preview))
                {
                    sb.AppendLine("```");
                    sb.AppendLine(preview);
                    sb.AppendLine("```");
                }
                else if (IsImage(stored.Name))
                {
                    sb.AppendLine($"  （图片附件，访问路径: /v1/files/{att.FileId}）");
                }
                else
                {
                    sb.AppendLine("  （二进制附件，网关已保存，当前未解析正文）");
                }
            }
        }

        return sb.ToString().Trim();
    }

    private static bool IsImage(string name)
    {
        var ext = Path.GetExtension(name).ToLowerInvariant();
        return ext is ".png" or ".jpg" or ".jpeg" or ".gif" or ".webp" or ".bmp";
    }
}
