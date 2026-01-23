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
}
