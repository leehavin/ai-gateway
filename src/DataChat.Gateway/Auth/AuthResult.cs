namespace DataChat.Gateway.Auth;

public sealed class AuthResult
{
    public required string Token { get; init; }
    public required string UserId { get; init; }
    public required string UserName { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
}
