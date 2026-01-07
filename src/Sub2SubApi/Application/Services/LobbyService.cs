using Sub2SubApi.Application.Models;
using Sub2SubApi.Application.Services;
using Sub2SubApi.Data;

namespace Sub2SubApi.Application;

public sealed class LobbyService : ILobbyService
{
    private readonly LobbyRepository _repo;

    private static readonly string[] RoleOrder = ["TOP", "JUNGLE", "MID", "ADC", "SUPPORT"];

    public LobbyService(LobbyRepository repo)
    {
        _repo = repo;
    }

    public async Task<LobbyDto> GetLobbyAsync(string lobbyId)
    {
        // If lobby doesn't exist yet, create a default "implicit" lobby.
        var meta = await _repo.GetLobbyMetaAsync(lobbyId);
        if (meta is null)
        {
            var startsAtIso = DateTimeOffset.UtcNow.AddMinutes(5).ToString("O");
            await _repo.PutLobbyMetaAsync(lobbyId, "Bronze War", startsAtIso);
            meta = ("Bronze War", startsAtIso);
        }

        var topBids = await _repo.GetAllTopBidsAsync(lobbyId);

        TeamDto BuildTeam(int teamIndex, string name)
        {
            var slots = RoleOrder.Select(role =>
            {
                var bid = topBids.TryGetValue((teamIndex, role), out var b) ? b : 0;

                return new SlotDto(
                    Role: role,
                    DisplayName: null,
                    AvatarUrl: null,
                    TopBidCredits: bid
                );
            }).ToArray();

            return new TeamDto(name, slots);
        }

        return new LobbyDto(
            LobbyId: lobbyId,
            TournamentName: meta.Value.TournamentName,
            StartsAtIso: meta.Value.StartsAtIso,
            Teams: new[]
            {
                BuildTeam(0, "Team A"),
                BuildTeam(1, "Team B"),
            }
        );
    }

    public async Task<BidResponse> PlaceBidAsync(string lobbyId, BidRequest request, string bidderUserId)
    {
        // Validate basics
        if (request.TeamIndex is < 0 or > 1)
            return new BidResponse(false, false, 0, 0);

        if (string.IsNullOrWhiteSpace(request.Role))
            return new BidResponse(false, false, 0, 0);

        if (request.Amount <= 0)
            return new BidResponse(false, false, 0, 0);

        var (accepted, currentTop) = await _repo.TryPlaceTopBidAsync(
            lobbyId,
            request.TeamIndex,
            request.Role,
            request.Amount,
            bidderUserId
        );

        // QueuePosition is a placeholder until you implement real queues.
        var queuePos = accepted ? 1 : 3;

        return new BidResponse(
            Accepted: accepted,
            DidBecomeTopBidder: accepted,
            CurrentTopBidCredits: currentTop,
            QueuePosition: queuePos
        );
    }

    // public Task<MatchResultDto> GetMatchResultAsync(string lobbyId, string userId){}
}
