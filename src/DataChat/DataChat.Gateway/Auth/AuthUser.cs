namespace DataChat.Gateway.Auth;

public sealed class AuthUser
{
    public required string UserId { get; init; }
    public required string UserName { get; init; }
    public bool IsSharedToken { get; init; }
}
