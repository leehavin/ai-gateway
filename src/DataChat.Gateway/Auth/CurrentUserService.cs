namespace DataChat.Gateway.Auth;

public interface ICurrentUserService
{
    AuthUser? Current { get; }
    string? UserId { get; }
}

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor) =>
        _httpContextAccessor = httpContextAccessor;

    public AuthUser? Current
    {
        get
        {
            var ctx = _httpContextAccessor.HttpContext;
            if (ctx is null) return null;
            return ctx.Items.TryGetValue(GatewayAuthConstants.HttpContextUserKey, out var value)
                && value is AuthUser user
                ? user
                : null;
        }
    }

    public string? UserId
    {
        get
        {
            var user = Current;
            if (user is null || user.IsSharedToken)
                return null;
            return user.UserId;
        }
    }
}
