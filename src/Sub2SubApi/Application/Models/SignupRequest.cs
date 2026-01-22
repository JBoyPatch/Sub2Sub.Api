namespace Sub2SubApi.Application.Models;

public sealed class SignupRequest
{
    public required string Username { get; init; }
    public required string Email { get; init; }
    // PasswordHash: client-provided already-hashed password
    public required string PasswordHash { get; init; }
}
