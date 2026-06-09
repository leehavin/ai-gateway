using DataChat.Gateway.Configuration;
using Microsoft.Extensions.Options;

namespace DataChat.Gateway.Auth;

public sealed class GatewayAuthService
{
    private readonly GatewayOptions _options;
    private readonly SessionTokenService _tokens;
    private readonly IHostAuthProvider _provider;

    public GatewayAuthService(
        IOptions<GatewayOptions> options,
        SessionTokenService tokens,
        IHostAuthProvider provider)
    {
        _options = options.Value;
        _tokens = tokens;
        _provider = provider;
    }

    public bool IsAuthEnabled =>
        _options.ValidTokens.Length > 0
        || _tokens.IsConfigured
        || !string.IsNullOrWhiteSpace(_options.Auth.ServiceKey);

    public async Task<AuthResult?> LoginAsync(string username, string password, CancellationToken cancellationToken)
    {
        var user = await _provider.ValidateCredentialsAsync(username, password, cancellationToken);
        return user is null || !_tokens.IsConfigured ? null : _tokens.Issue(user);
    }

    public AuthResult? IssueTrustedUser(string serviceKey, string userId, string? userName)
    {
        if (!IsTrustedServiceKey(serviceKey) || !_tokens.IsConfigured)
            return null;
        if (string.IsNullOrWhiteSpace(userId))
            return null;

        var user = new AuthUser
        {
            UserId = userId.Trim(),
            UserName = string.IsNullOrWhiteSpace(userName) ? userId.Trim() : userName.Trim()
        };
        return _tokens.Issue(user);
    }

    public AuthUser? ValidateBearer(string bearerToken)
    {
        if (string.IsNullOrWhiteSpace(bearerToken))
            return null;

        var token = bearerToken.Trim();

        var sessionUser = _tokens.Validate(token);
        if (sessionUser is not null)
            return sessionUser;

        if (_options.ValidTokens.Contains(token))
        {
            return new AuthUser
            {
                UserId = "__shared__",
                UserName = "共享",
                IsSharedToken = true
            };
        }

        return null;
    }

    public bool IsTrustedServiceKey(string? serviceKey) =>
        !string.IsNullOrWhiteSpace(_options.Auth.ServiceKey)
        && string.Equals(_options.Auth.ServiceKey.Trim(), serviceKey?.Trim(), StringComparison.Ordinal);

    public AuthUser? GetUserFromHttpContext(HttpContext context) =>
        context.Items.TryGetValue(GatewayAuthConstants.HttpContextUserKey, out var value)
        && value is AuthUser user
            ? user
            : null;
}
