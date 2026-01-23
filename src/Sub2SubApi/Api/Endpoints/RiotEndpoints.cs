using System.Text.Json;
using Sub2SubApi.Application.Services;
using Sub2SubApi.Application.Models;
using Sub2SubApi.Infrastructure.Auth;

namespace Sub2SubApi.Api.Endpoints;

public static class RiotEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static void Map(ApiRouter router, IRiotService riotService)
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
    }

    private sealed class LinkRequest
    {
        public string? GameName { get; set; }
        public string? TagLine { get; set; }
    }
}
