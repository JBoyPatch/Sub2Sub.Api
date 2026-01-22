namespace Sub2SubApi.Application.Models;

public sealed class LoginRequest
{
    public required string Username { get; init; }
    // PasswordHash: client-provided already-hashed password
    public required string PasswordHash { get; init; }
}
