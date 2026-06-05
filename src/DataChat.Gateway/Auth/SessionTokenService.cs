using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DataChat.Gateway.Configuration;
using Microsoft.Extensions.Options;

namespace DataChat.Gateway.Auth;

public sealed class SessionTokenService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly GatewayAuthOptions _options;

    public SessionTokenService(IOptions<GatewayOptions> options) => _options = options.Value.Auth;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.SigningKey);

    public AuthResult Issue(AuthUser user)
    {
        var signingKey = RequireSigningKey();
        var lifetime = TimeSpan.FromHours(Math.Clamp(_options.TokenLifetimeHours, 1, 24 * 30));
        var expiresAt = DateTimeOffset.UtcNow.Add(lifetime);
        var payload = new TokenPayload
        {
            Sub = user.UserId,
            Name = user.UserName,
            Exp = expiresAt.ToUnixTimeSeconds()
        };
        var token = Sign(payload, signingKey);
        return new AuthResult
        {
            Token = token,
            UserId = user.UserId,
            UserName = user.UserName,
            ExpiresAt = expiresAt
        };
    }

    public AuthUser? Validate(string token)
    {
        if (!IsConfigured || string.IsNullOrWhiteSpace(token))
            return null;

        var parts = token.Split('.', 2);
        if (parts.Length != 2)
            return null;

        byte[] payloadBytes;
        try
        {
            payloadBytes = Base64UrlDecode(parts[0]);
        }
        catch
        {
            return null;
        }

        var signingKey = _options.SigningKey.Trim();
        var expectedSig = ComputeSignature(payloadBytes, signingKey);
        byte[] actualSig;
        try
        {
            actualSig = Base64UrlDecode(parts[1]);
        }
        catch
        {
            return null;
        }

        if (!CryptographicOperations.FixedTimeEquals(expectedSig, actualSig))
            return null;

        TokenPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<TokenPayload>(payloadBytes, JsonOptions);
        }
        catch
        {
            return null;
        }

        if (payload is null
            || string.IsNullOrWhiteSpace(payload.Sub)
            || payload.Exp <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            return null;

        return new AuthUser
        {
            UserId = payload.Sub.Trim(),
            UserName = string.IsNullOrWhiteSpace(payload.Name) ? payload.Sub.Trim() : payload.Name.Trim()
        };
    }

    private string RequireSigningKey()
    {
        if (!IsConfigured)
            throw new InvalidOperationException("Gateway:Auth:SigningKey 未配置，无法签发用户 Token。");
        return _options.SigningKey.Trim();
    }

    private static string Sign(TokenPayload payload, string signingKey)
    {
        var payloadBytes = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);
        var sig = ComputeSignature(payloadBytes, signingKey);
        return $"{Base64UrlEncode(payloadBytes)}.{Base64UrlEncode(sig)}";
    }

    private static byte[] ComputeSignature(byte[] payloadBytes, string signingKey)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(signingKey));
        return hmac.ComputeHash(payloadBytes);
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string input)
    {
        var padded = input.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }
        return Convert.FromBase64String(padded);
    }

    private sealed class TokenPayload
    {
        public string Sub { get; set; } = "";
        public string Name { get; set; } = "";
        public long Exp { get; set; }
    }
}
