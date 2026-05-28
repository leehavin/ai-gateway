using DataChat.Core.Abstractions;

namespace DataChat.Infrastructure.Security;

/// <summary>
/// V1：用户目录加密文件存 API Key。生产可换 Credential Manager。
/// </summary>
public sealed class FileApiKeyStore : IApiKeyStore
{
    private readonly string _folder;

    public FileApiKeyStore()
    {
        _folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DataChat", "keys");
        Directory.CreateDirectory(_folder);
    }

    public Task<string?> GetAsync(string keyRef, CancellationToken cancellationToken = default)
    {
        var path = GetPath(keyRef);
        if (!File.Exists(path))
            return Task.FromResult<string?>(null);
        var bytes = File.ReadAllBytes(path);
        var plain = System.Security.Cryptography.ProtectedData.Unprotect(
            bytes, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
        return Task.FromResult<string?>(System.Text.Encoding.UTF8.GetString(plain));
    }

    public Task SetAsync(string keyRef, string apiKey, CancellationToken cancellationToken = default)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(apiKey);
        var protectedBytes = System.Security.Cryptography.ProtectedData.Protect(
            bytes, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
        File.WriteAllBytes(GetPath(keyRef), protectedBytes);
        return Task.CompletedTask;
    }

    private string GetPath(string keyRef) =>
        Path.Combine(_folder, $"{Sanitize(keyRef)}.key");

    private static string Sanitize(string keyRef) =>
        string.Concat(keyRef.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
}
