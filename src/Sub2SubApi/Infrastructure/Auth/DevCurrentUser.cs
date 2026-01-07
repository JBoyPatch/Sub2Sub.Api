using System.Web;
using Amazon.Lambda.APIGatewayEvents;

namespace Sub2SubApi.Infrastructure.Auth;

public sealed class DevCurrentUser : ICurrentUser
{
    public string UserId { get; private set; } = "NA-User";
    public string DisplayName { get; private set; } = "N/A User";
    public string? AvatarUrl { get; private set; } = null;

    /// <summary>
    /// Dev auth: resolve user from query params or headers.
    /// Query params win over headers.
    /// </summary>
    public static DevCurrentUser FromRequest(APIGatewayHttpApiV2ProxyRequest request)
    {
        var qp = request.QueryStringParameters ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var headers = request.Headers ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Helpers
        static string? Get(IDictionary<string, string> dict, string key)
            => dict.TryGetValue(key, out var v) ? v : null;

        // Prefer query params
        var userId =
            Get(qp, "userId") ??
            Get(headers, "x-user-id") ??
            "dev-user";

        var displayName =
            Get(qp, "displayName") ??
            Get(headers, "x-display-name") ??
            userId; // fallback to userId

        var avatarUrl =
            Get(qp, "avatarUrl") ??
            Get(headers, "x-avatar-url");

        // Some clients URL-encode query params; decode if needed
        userId = HttpUtility.UrlDecode(userId);
        displayName = HttpUtility.UrlDecode(displayName);
        avatarUrl = avatarUrl is null ? null : HttpUtility.UrlDecode(avatarUrl);

        return new DevCurrentUser
        {
            UserId = userId,
            DisplayName = displayName,
            AvatarUrl = avatarUrl
        };
    }
}
