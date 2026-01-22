namespace Sub2SubApi.Application.Models;

public sealed class AuthResponse
{
    public required bool Ok { get; init; }
    public string? Username { get; init; }
    public string? Email { get; init; }
    public string? AvatarUrl { get; init; }
    public int? Credits { get; init; }
    public string? Type { get; init; }
    public string? Message { get; init; }
}
