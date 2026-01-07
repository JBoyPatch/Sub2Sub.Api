namespace Sub2SubApi.Infrastructure.Auth;

public sealed class DevCurrentUser : ICurrentUser
{
    // Later: derive from JWT / Twitch identity
    public string UserId => "dev-user-1";
}
