using System.Text.Json;
using Sub2SubApi.Application.Models;
using Sub2SubApi.Application.Services;
using Sub2SubApi.Infrastructure.Auth;

namespace Sub2SubApi.Api.Endpoints;

public static class LobbyEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static void Map(ApiRouter router, ILobbyService lobbyService)
    {
        // GET /lobbies/{lobbyId}
        router.Map("GET", "/lobbies/(?<lobbyId>[A-Za-z0-9_-]+)", async ctx =>
        {
            var lobbyId = ctx.GetRouteParam("lobbyId");
            if (string.IsNullOrWhiteSpace(lobbyId))
                return HttpResults.BadRequest(new { message = "LobbyId required" });

            var lobby = await lobbyService.GetLobbyAsync(lobbyId);
            return HttpResults.Ok(lobby);
        });

        // POST /lobbies/{lobbyId}/bids
        router.Map("POST", "/lobbies/(?<lobbyId>[A-Za-z0-9_-]+)/bids", async ctx =>
        {
            var lobbyId = ctx.GetRouteParam("lobbyId");
            if (string.IsNullOrWhiteSpace(lobbyId))
                return HttpResults.BadRequest(new { message = "LobbyId required" });

            var body = ctx.Request.Body;
            if (string.IsNullOrWhiteSpace(body))
                return HttpResults.BadRequest(new { message = "Body required" });

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
                return HttpResults.BadRequest(new { message = "Invalid bid request" });

            var user = DevCurrentUser.FromRequest(ctx.Request);

            var result = await lobbyService.PlaceBidAsync(
                lobbyId,
                req,
                user.UserId,
                user.DisplayName,
                user.AvatarUrl
            );
            return HttpResults.Ok(result);
        });

        // GET /lobbies/{lobbyId}/result
        // router.Map("GET", "/lobbies/(?<lobbyId>[A-Za-z0-9_-]+)/result", async ctx =>
        // {
        //     var lobbyId = ctx.GetRouteParam("lobbyId")!;
        //     var result = await lobbyService.GetMatchResultAsync(lobbyId, currentUser.UserId);

        //     return HttpResults.Ok(result);
        // });

        // OPTIONS - CORS preflight (simple catch-all)
        router.Map("OPTIONS", "/.*", _ =>
        {
            return Task.FromResult(HttpResults.Json(200, new { ok = true }));
        });
    }
}
