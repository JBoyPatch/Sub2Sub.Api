namespace Sub2SubApi.Infrastructure.Auth;

public interface ICurrentUser
{
    string UserId { get; }
    string DisplayName { get; }
    string? AvatarUrl { get; }
}
