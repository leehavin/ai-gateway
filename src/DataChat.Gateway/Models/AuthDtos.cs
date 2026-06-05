namespace DataChat.Gateway.Models;

public sealed class LoginRequest
{
    public string? Username { get; set; }
    public string? Password { get; set; }
}

public sealed class IssueTokenRequest
{
    public string? ServiceKey { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
}

public sealed class AuthResponse
{
    public required string Token { get; set; }
    public required string UserId { get; set; }
    public required string UserName { get; set; }
    public long ExpiresAt { get; set; }
}

public sealed class AuthMeResponse
{
    public required string UserId { get; set; }
    public required string UserName { get; set; }
    public bool IsSharedToken { get; set; }
}
