using System.Text.Json;
using Sub2SubApi.Application.Services;
using Sub2SubApi.Application.Models;
using Sub2SubApi.Infrastructure.Auth;

namespace Sub2SubApi.Api.Endpoints;

public static class RiotEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static void Map(ApiRouter router, IRiotService riotService, IAuthService authService)
    {
        // POST /users/{userId}/riot/link
        router.Map("POST", "/users/(?<userId>[A-Za-z0-9_-]+)/riot/link", async ctx =>
        {
            var userId = ctx.GetRouteParam("userId");
            if (string.IsNullOrWhiteSpace(userId)) return HttpResults.BadRequest(new { message = "userId required" });

            var body = ctx.Request.Body;
            if (string.IsNullOrWhiteSpace(body)) return HttpResults.BadRequest(new { message = "Body required" });

            LinkRequest? req;
            try { req = JsonSerializer.Deserialize<LinkRequest>(body, JsonOptions); }
            catch { return HttpResults.BadRequest(new { message = "Invalid JSON body" }); }

            if (req is null || string.IsNullOrWhiteSpace(req.GameName) || string.IsNullOrWhiteSpace(req.TagLine))
                return HttpResults.BadRequest(new { message = "gameName and tagLine required" });

            try
            {
                var profile = await riotService.LinkRiotAccountAsync(userId, req.GameName, req.TagLine);
                return HttpResults.Ok(profile);
            }
            catch (ArgumentException ex)
            {
                return HttpResults.BadRequest(new { message = ex.Message });
            }
        });

        // POST /users/{userId}/riot/sync-ranked
        router.Map("POST", "/users/(?<userId>[A-Za-z0-9_-]+)/riot/sync-ranked", async ctx =>
        {
            var userId = ctx.GetRouteParam("userId");
            if (string.IsNullOrWhiteSpace(userId)) return HttpResults.BadRequest(new { message = "userId required" });
            await riotService.SyncRankedAsync(userId);
            return HttpResults.Ok(new { ok = true });
        });

        // POST /users/{userId}/riot/sync-mastery
        router.Map("POST", "/users/(?<userId>[A-Za-z0-9_-]+)/riot/sync-mastery", async ctx =>
        {
            var userId = ctx.GetRouteParam("userId");
            if (string.IsNullOrWhiteSpace(userId)) return HttpResults.BadRequest(new { message = "userId required" });
            await riotService.SyncChampionMasteryAsync(userId);
            return HttpResults.Ok(new { ok = true });
        });

        // POST /users/{userId}/riot/sync-matches
        router.Map("POST", "/users/(?<userId>[A-Za-z0-9_-]+)/riot/sync-matches", async ctx =>
        {
            var userId = ctx.GetRouteParam("userId");
            if (string.IsNullOrWhiteSpace(userId)) return HttpResults.BadRequest(new { message = "userId required" });
            await riotService.SyncMatchesAsync(userId);
            return HttpResults.Ok(new { ok = true });
        });

        // POST /users/{userId}/riot/sync (sync-all)
        router.Map("POST", "/users/(?<userId>[A-Za-z0-9_-]+)/riot/sync", async ctx =>
        {
            var userId = ctx.GetRouteParam("userId");
            if (string.IsNullOrWhiteSpace(userId)) return HttpResults.BadRequest(new { message = "userId required" });
            await riotService.SyncAllAsync(userId);
            return HttpResults.Ok(new { ok = true });
        });

        // GET /users/{userId}/riot/profile
        router.Map("GET", "/users/(?<userId>[A-Za-z0-9_-]+)/riot/profile", async ctx =>
        {
            var userId = ctx.GetRouteParam("userId");
            if (string.IsNullOrWhiteSpace(userId)) return HttpResults.BadRequest(new { message = "userId required" });
            var profile = await riotService.GetProfileAsync(userId);
            if (profile is null) return HttpResults.NotFound(new { message = "Riot profile not found" });
            return HttpResults.Ok(profile);
        });

        // GET /users/{userId}/riot/ranked
        router.Map("GET", "/users/(?<userId>[A-Za-z0-9_-]+)/riot/ranked", async ctx =>
        {
            var userId = ctx.GetRouteParam("userId");
            if (string.IsNullOrWhiteSpace(userId)) return HttpResults.BadRequest(new { message = "userId required" });
            var ranked = await riotService.GetRankedAsync(userId);
            return HttpResults.Ok(ranked);
        });

        // GET /users/{userId}/riot/mastery?top=N
        router.Map("GET", "/users/(?<userId>[A-Za-z0-9_-]+)/riot/mastery", async ctx =>
        {
            var userId = ctx.GetRouteParam("userId");
            if (string.IsNullOrWhiteSpace(userId)) return HttpResults.BadRequest(new { message = "userId required" });
            var qs = ctx.Request.QueryStringParameters ?? new Dictionary<string, string>();
            var top = 0;
            if (qs.TryGetValue("top", out var topv) && int.TryParse(topv, out var parsed)) top = parsed;
            var mastery = await riotService.GetChampionMasteryAsync(userId, top);
            return HttpResults.Ok(mastery);
        });

        // GET /users/{userId}/riot/matches?count=N
        router.Map("GET", "/users/(?<userId>[A-Za-z0-9_-]+)/riot/matches", async ctx =>
        {
            var userId = ctx.GetRouteParam("userId");
            if (string.IsNullOrWhiteSpace(userId)) return HttpResults.BadRequest(new { message = "userId required" });
            var qs = ctx.Request.QueryStringParameters ?? new Dictionary<string, string>();
            var count = 20;
            if (qs.TryGetValue("count", out var cv) && int.TryParse(cv, out var parsed)) count = parsed;
            var matches = await riotService.GetMatchesAsync(userId, count);
            return HttpResults.Ok(matches);
        });

        // GET /users/{userId} - basic user info
        router.Map("GET", "/users/(?<userId>[A-Za-z0-9_-]+)", async ctx =>
        {
            var userId = ctx.GetRouteParam("userId");
            if (string.IsNullOrWhiteSpace(userId)) return HttpResults.BadRequest(new { message = "userId required" });
            var user = await authService.GetUserByIdAsync(userId);
            if (user is null) return HttpResults.NotFound(new { message = "User not found" });
            return HttpResults.Ok(new { Id = user.Id, Username = user.Username, Email = user.Email, AvatarUrl = user.AvatarUrl, Credits = user.Credits, Type = user.Type });
        });

        // GET /users/{userId}/profile-full?matches=10&mastery=5
        router.Map("GET", "/users/(?<userId>[A-Za-z0-9_-]+)/profile-full", async ctx =>
        {
            var userId = ctx.GetRouteParam("userId");
            if (string.IsNullOrWhiteSpace(userId)) return HttpResults.BadRequest(new { message = "userId required" });
            var qs = ctx.Request.QueryStringParameters ?? new Dictionary<string, string>();
            var matchesCount = 10; var masteryTop = 5;
            if (qs.TryGetValue("matches", out var mstr) && int.TryParse(mstr, out var mp)) matchesCount = mp;
            if (qs.TryGetValue("mastery", out var mst) && int.TryParse(mst, out var mt)) masteryTop = mt;

            var userTask = authService.GetUserByIdAsync(userId);
            var profileTask = riotService.GetProfileAsync(userId);
            var rankedTask = riotService.GetRankedAsync(userId);
            var masteryTask = riotService.GetChampionMasteryAsync(userId, masteryTop);
            var matchesTask = riotService.GetMatchesAsync(userId, matchesCount);

            await Task.WhenAll(userTask, profileTask, rankedTask, masteryTask, matchesTask);

            var user = userTask.Result;
            if (user is null) return HttpResults.NotFound(new { message = "User not found" });

            return HttpResults.Ok(new
            {
                User = new { Id = user.Id, Username = user.Username, Email = user.Email, AvatarUrl = user.AvatarUrl, Credits = user.Credits, Type = user.Type },
                RiotProfile = profileTask.Result,
                Ranked = rankedTask.Result,
                Mastery = masteryTask.Result,
                Matches = matchesTask.Result
            });
        });
    }

    private sealed class LinkRequest
    {
        public string? GameName { get; set; }
        public string? TagLine { get; set; }
    }
}
