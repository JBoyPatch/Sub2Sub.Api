namespace Sub2SubApi.Application.Models;

public sealed class UserDto
{
    // Server-generated unique id for the user (e.g. GUID)
    public required string Id { get; init; }
    public required string Username { get; init; }
    public required string Email { get; init; }
    // Stores server-side copy of provided password hash
    public required string PasswordHash { get; init; }
    // Optional avatar URL
    public string? AvatarUrl { get; init; }
    // Credits balance
    public int Credits { get; init; }
    // User type (e.g. "User", "Admin")
    public required string Type { get; init; }
}
