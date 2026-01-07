using Sub2SubApi.Application.Models;

namespace Sub2SubApi.Application.Services;

public sealed class LobbyService : ILobbyService
{
    public Task<LobbyDto> GetLobbyAsync(string lobbyId, string userId)
    {
        var now = DateTimeOffset.UtcNow;
        var startsAt = now.AddMinutes(2);

        var roles = new[] { "TOP", "JUNGLE", "MID", "ADC", "SUPPORT" };

        TeamDto MakeTeam(string name) =>
            new(name, roles.Select(r => new SlotDto(r, null, null, TopBidCredits: r == "TOP" ? 10 : 0)).ToArray());

        return Task.FromResult(new LobbyDto(
            LobbyId: lobbyId,
            TournamentName: "Bronze War",
            StartsAtIso: startsAt.ToString("O"),
            Teams: new[] { MakeTeam("Team A"), MakeTeam("Team B") }
        ));
    }

    public Task<BidResponse> PlaceBidAsync(string lobbyId, string userId, BidRequest request)
    {
        // Stub: accept any bid >= 1, pretend you became top bidder if amount >= 10
        var accepted = request.Amount >= 1;
        var didBecomeTop = request.Amount >= 10;

        return Task.FromResult(new BidResponse(
            Accepted: accepted,
            DidBecomeTopBidder: didBecomeTop,
            CurrentTopBidCredits: Math.Max(10, request.Amount),
            QueuePosition: 3
        ));
    }

    public Task<MatchResultDto> GetMatchResultAsync(string lobbyId, string userId)
    {
        // Stub: pretend user wins TOP with a demo code
        return Task.FromResult(new MatchResultDto(
            DidWin: true,
            WonRole: "TOP",
            MatchCode: "DEMO-ABCD-1234"
        ));
    }
}
