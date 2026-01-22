using Sub2SubApi.Application.Models;

namespace Sub2SubApi.Application.Services;

public interface ILobbyService
{
    Task<LobbyDto> GetLobbyAsync(string lobbyId);
    Task<string?> CreateLobbyAsync(string lobbyId, string tournamentName, string startsAtIso);
    Task<BidResponse> PlaceBidAsync(string lobbyId, BidRequest request, string bidderUserId, 
        string bidderDisplayName, string? bidderAvatarUrl);
    
    // Task<MatchResultDto> GetMatchResultAsync(string lobbyId, string userId);
}
