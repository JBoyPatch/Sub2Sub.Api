using System.Text.Json;
using Sub2SubApi.Application.Models;
using Sub2SubApi.Application.Services;
using Sub2SubApi.Infrastructure.Auth;

namespace Sub2SubApi.Api.Endpoints;

public static class LobbyEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static void Map(ApiRouter router, ILobbyService lobbyService, ICurrentUser currentUser)
    {
        // GET /lobbies/{lobbyId}
        router.Map("GET", "/lobbies/(?<lobbyId>[A-Za-z0-9_-]+)", async ctx =>
        {
            var lobbyId = ctx.GetRouteParam("lobbyId")!;
            var lobby = await lobbyService.GetLobbyAsync(lobbyId, currentUser.UserId);

            return HttpResults.Ok(lobby);
        });

        // POST /lobbies/{lobbyId}/bids
        router.Map("POST", "/lobbies/(?<lobbyId>[A-Za-z0-9_-]+)/bids", async ctx =>
        {
            var lobbyId = ctx.GetRouteParam("lobbyId")!;
            var body = ctx.Request.Body ?? "";

            BidRequest? req;
            try
            {
                req = JsonSerializer.Deserialize<BidRequest>(body, JsonOptions);
            }
            catch
            {
                return HttpResults.BadRequest(new { message = "Invalid JSON body" });
            }

            if (req is null)
                return HttpResults.BadRequest(new { message = "Body required" });

            var result = await lobbyService.PlaceBidAsync(lobbyId, currentUser.UserId, req);
            return HttpResults.Ok(result);
        });

        // GET /lobbies/{lobbyId}/result
        router.Map("GET", "/lobbies/(?<lobbyId>[A-Za-z0-9_-]+)/result", async ctx =>
        {
            var lobbyId = ctx.GetRouteParam("lobbyId")!;
            var result = await lobbyService.GetMatchResultAsync(lobbyId, currentUser.UserId);

            return HttpResults.Ok(result);
        });

        // OPTIONS - CORS preflight (simple catch-all)
        router.Map("OPTIONS", "/.*", _ =>
        {
            return Task.FromResult(HttpResults.Json(200, new { ok = true }));
        });
    }
}
