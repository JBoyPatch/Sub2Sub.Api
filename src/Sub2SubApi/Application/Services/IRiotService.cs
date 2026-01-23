using System.Threading.Tasks;
using Sub2SubApi.Application.Models;
using System.Collections.Generic;

namespace Sub2SubApi.Application.Services;

public interface IRiotService
{
    Task<RiotProfileDto> LinkRiotAccountAsync(string userId, string gameName, string tagLine);
    Task SyncRankedAsync(string userId);
    Task SyncChampionMasteryAsync(string userId, int topN = 5);
    Task SyncMatchesAsync(string userId, int count = 20);
    Task SyncAllAsync(string userId);
    Task<RiotProfileDto?> GetProfileAsync(string userId, int recentMatches = 10);
    Task<IEnumerable<UserRankedStatsDto>> GetRankedAsync(string userId);
    Task<IEnumerable<UserChampionMasteryDto>> GetChampionMasteryAsync(string userId, int topN = 0);
    Task<IEnumerable<UserMatchStatsDto>> GetMatchesAsync(string userId, int count = 20);
}
