using Sub2SubApi.Application.Models;

namespace Sub2SubApi.Application.Services;

public interface ILobbyService
{
    Task<LobbyDto> GetLobbyAsync(string lobbyId, string userId);
    Task<BidResponse> PlaceBidAsync(string lobbyId, string userId, BidRequest request);
    Task<MatchResultDto> GetMatchResultAsync(string lobbyId, string userId);
}
