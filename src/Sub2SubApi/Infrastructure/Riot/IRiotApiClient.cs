using System.Collections.Generic;
using System.Threading.Tasks;
using Sub2SubApi.Application.Models;

namespace Sub2SubApi.Infrastructure.Riot;

public interface IRiotApiClient
{
    Task<RiotAccountDto?> GetAccountByRiotIdAsync(string gameName, string tagLine);
    Task<RiotSummonerDto?> GetSummonerByPuuidAsync(string puuid);
    Task<IReadOnlyList<RiotRankedEntryDto>> GetRankedEntriesBySummonerIdAsync(string summonerId);
    Task<IReadOnlyList<RiotChampionMasteryDto>> GetChampionMasteryByPuuidAsync(string puuid);
    Task<IReadOnlyList<string>> GetMatchIdsByPuuidAsync(string puuid, int start, int count);
    Task<RiotMatchDto> GetMatchByIdAsync(string matchId);
}
