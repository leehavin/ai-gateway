using System.Text;
using DataChat.Gateway.Configuration;
using DataChat.Gateway.Models;
using Microsoft.Extensions.Options;

namespace DataChat.Gateway.Services;

public sealed class FileStorageService
{
    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".md", ".csv", ".json", ".xml", ".log", ".yaml", ".yml", ".html", ".htm"
    };

    private readonly GatewayOptions _options;
    private readonly ILogger<FileStorageService> _logger;
    private readonly string _root;

    public FileStorageService(IOptions<GatewayOptions> options, ILogger<FileStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _root = Path.IsPathRooted(_options.UploadDirectory)
            ? _options.UploadDirectory
            : Path.Combine(AppContext.BaseDirectory, _options.UploadDirectory);
        Directory.CreateDirectory(_root);
    }

    public long MaxBytes => _options.MaxUploadSizeMb * 1024L * 1024L;

    public async Task<FileUploadResponse> SaveAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length <= 0)
            throw new InvalidOperationException("文件为空。");
        if (file.Length > MaxBytes)
            throw new InvalidOperationException($"文件超过最大 {_options.MaxUploadSizeMb} MB。");

        var fileId = Guid.NewGuid().ToString("N");
        var safeName = Path.GetFileName(file.FileName);
        if (string.IsNullOrWhiteSpace(safeName))
            safeName = "file";

        var storedName = $"{fileId}_{safeName}";
        var path = Path.Combine(_root, storedName);

        await using (var stream = File.Create(path))
            await file.CopyToAsync(stream, cancellationToken);

        var contentType = string.IsNullOrWhiteSpace(file.ContentType)
            ? "application/octet-stream"
            : file.ContentType;

        var preview = await TryReadTextPreviewAsync(path, safeName, cancellationToken);

        _logger.LogInformation("File uploaded id={FileId} name={Name} size={Size}", fileId, safeName, file.Length);

        return new FileUploadResponse
        {
            FileId = fileId,
            Name = safeName,
            ContentType = contentType,
            Size = file.Length,
            Url = $"/v1/files/{fileId}",
            TextPreview = preview
        };
    }

    public async Task<FileUploadResponse> RegisterFromPathAsync(string sourcePath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
            throw new InvalidOperationException("文件路径不能为空。");

        var fullPath = Path.GetFullPath(sourcePath.Trim());
        if (!File.Exists(fullPath))
            throw new InvalidOperationException($"文件不存在: {fullPath}");

        var info = new FileInfo(fullPath);
        if (info.Length <= 0)
            throw new InvalidOperationException("文件为空。");
        if (info.Length > MaxBytes)
            throw new InvalidOperationException($"文件超过最大 {_options.MaxUploadSizeMb} MB。");

        var safeName = Path.GetFileName(fullPath);
        if (string.IsNullOrWhiteSpace(safeName))
            safeName = "file";

        var fileId = Guid.NewGuid().ToString("N");
        var storedName = $"{fileId}_{safeName}";
        var destPath = Path.Combine(_root, storedName);

        await using (var src = File.OpenRead(fullPath))
        await using (var dest = File.Create(destPath))
            await src.CopyToAsync(dest, cancellationToken);

        var preview = await TryReadTextPreviewAsync(destPath, safeName, cancellationToken);
        _logger.LogInformation(
            "File registered from path id={FileId} name={Name} size={Size} source={Source}",
            fileId,
            safeName,
            info.Length,
            fullPath);

        return new FileUploadResponse
        {
            FileId = fileId,
            Name = safeName,
            ContentType = "application/octet-stream",
            Size = info.Length,
            Url = $"/v1/files/{fileId}",
            TextPreview = preview
        };
    }

    public StoredFile? Get(string fileId)
    {
        if (string.IsNullOrWhiteSpace(fileId) || fileId.Length > 64)
            return null;

        var matches = Directory.GetFiles(_root, $"{fileId}_*");
        if (matches.Length == 0)
            return null;

        var path = matches[0];
        var name = Path.GetFileName(path)[(fileId.Length + 1)..];
        return new StoredFile(path, name);
    }

    public async Task<string?> TryReadTextPreviewAsync(string path, string name, CancellationToken cancellationToken)
    {
        var ext = Path.GetExtension(name);
        if (!TextExtensions.Contains(ext))
            return null;

        try
        {
            await using var stream = File.OpenRead(path);
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var text = await reader.ReadToEndAsync(cancellationToken);
            var max = _options.MaxUploadTextPreviewChars;
            if (text.Length > max)
                return text[..max] + "\n…(内容已截断)";
            return text;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Text preview failed for {Name}", name);
            return null;
        }
    }

    public sealed record StoredFile(string Path, string Name);
}
