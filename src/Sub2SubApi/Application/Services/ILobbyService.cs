using Sub2SubApi.Application.Models;

namespace Sub2SubApi.Application.Services;

public interface ILobbyService
{
    Task<LobbyDto> GetLobbyAsync(string lobbyId);
    Task<BidResponse> PlaceBidAsync(string lobbyId, BidRequest request, string bidderUserId);
    
    // Task<MatchResultDto> GetMatchResultAsync(string lobbyId, string userId);
}
