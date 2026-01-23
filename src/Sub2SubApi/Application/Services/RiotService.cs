using System;
using System.Threading.Tasks;
using Sub2SubApi.Application.Models;
using Sub2SubApi.Data;
using Sub2SubApi.Infrastructure.Riot;

namespace Sub2SubApi.Application.Services;

public sealed class RiotService : IRiotService
{
    private readonly IRiotApiClient _client;
    private readonly UserRiotRepository _userRepo;
    private readonly MatchRepository _matchRepo;

    public RiotService(IRiotApiClient client, UserRiotRepository userRepo, MatchRepository matchRepo)
    {
        _client = client;
        _userRepo = userRepo;
        _matchRepo = matchRepo;
    }

    public async Task<RiotProfileDto> LinkRiotAccountAsync(string userId, string gameName, string tagLine)
    {
        // Resolve account -> puuid
        var account = await _client.GetAccountByRiotIdAsync(gameName, tagLine);
        if (account is null) throw new ArgumentException("Riot account not found");

        var summoner = await _client.GetSummonerByPuuidAsync(account.Puuid);

        var profile = new RiotProfileDto
        {
            UserId = userId,
            RiotPuuid = account.Puuid,
            RiotGameName = gameName,
            RiotTagline = tagLine,
            RiotSummonerId = summoner?.SummonerId,
            RiotProfileIconId = summoner?.ProfileIconId,
            RiotSummonerLevel = summoner?.SummonerLevel,
            LastRiotProfileSyncAtEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        await _userRepo.UpsertRiotProfileAsync(profile);
        return profile;
    }

    public Task SyncRankedAsync(string userId)
    {
        return SyncRankedInternalAsync(userId);
    }

    public Task SyncChampionMasteryAsync(string userId, int topN = 5)
    {
        return SyncChampionMasteryInternalAsync(userId, topN);
    }

    public Task SyncMatchesAsync(string userId, int count = 20)
    {
        return SyncMatchesInternalAsync(userId, count);
    }

    public async Task SyncAllAsync(string userId)
    {
        await SyncRankedAsync(userId);
        await SyncChampionMasteryAsync(userId);
        await SyncMatchesAsync(userId);
    }

    public Task<RiotProfileDto?> GetProfileAsync(string userId, int recentMatches = 10)
    {
        return _userRepo.GetRiotProfileAsync(userId);
    }

    private async Task SyncRankedInternalAsync(string userId)
    {
        var profile = await _userRepo.GetRiotProfileAsync(userId);
        if (profile is null) throw new ArgumentException("Riot profile not linked");
        if (string.IsNullOrWhiteSpace(profile.RiotSummonerId)) throw new ArgumentException("Riot summoner id missing");

        var entries = await _client.GetRankedEntriesBySummonerIdAsync(profile.RiotSummonerId!);
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        foreach (var e in entries)
        {
            var dto = new UserRankedStatsDto
            {
                UserId = userId,
                QueueType = e.QueueType,
                Tier = e.Tier,
                Rank = e.Rank,
                LeaguePoints = e.LeaguePoints,
                Wins = e.Wins,
                Losses = e.Losses,
                LastSyncedAtEpoch = now
            };
            await _userRepo.UpsertRankedEntryAsync(dto);
        }
    }

    private async Task SyncChampionMasteryInternalAsync(string userId, int topN)
    {
        var profile = await _userRepo.GetRiotProfileAsync(userId);
        if (profile is null) throw new ArgumentException("Riot profile not linked");

        var masteries = await _client.GetChampionMasteryByPuuidAsync(profile.RiotPuuid);
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var top = masteries.Take(topN);
        foreach (var m in top)
        {
            var dto = new UserChampionMasteryDto
            {
                UserId = userId,
                ChampionId = m.ChampionId,
                ChampionPoints = m.ChampionPoints,
                ChampionLevel = m.ChampionLevel,
                LastPlayTimeEpoch = m.LastPlayTime,
                ChestGranted = m.ChestGranted,
                LastSyncedAtEpoch = now
            };
            await _userRepo.UpsertChampionMasteryAsync(dto);
        }
    }

    private async Task SyncMatchesInternalAsync(string userId, int count)
    {
        var profile = await _userRepo.GetRiotProfileAsync(userId);
        if (profile is null) throw new ArgumentException("Riot profile not linked");

        var matchIds = await _client.GetMatchIdsByPuuidAsync(profile.RiotPuuid, 0, count);
        foreach (var matchId in matchIds)
        {
            var exists = await _matchRepo.ExistsUserMatchAsync(userId, matchId);
            if (exists) continue;

            // fetch match details
            var match = await _client.GetMatchByIdAsync(matchId);

            // parse match JSON to extract global fields and participant row
            if (string.IsNullOrWhiteSpace(match.RawJson)) continue;

            using var doc = JsonDocument.Parse(match.RawJson!);
            var info = doc.RootElement.GetProperty("info");
            var gameStart = info.GetProperty("gameStartTimestamp").GetInt64();
            var gameDuration = info.GetProperty("gameDuration").GetInt64();
            var queueId = info.GetProperty("queueId").GetInt32();

            var matchDto = new MatchDto
            {
                MatchId = matchId,
                GameStartTimestamp = gameStart,
                GameDurationSeconds = gameDuration,
                QueueId = queueId,
                RawJson = match.RawJson,
                CreatedAtEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            // Try create global match record; ignore if exists
            try { await _matchRepo.TryUpsertGlobalMatchAsync(matchDto); } catch { /* ignore */ }

            // find our participant
            foreach (var participant in info.GetProperty("participants").EnumerateArray())
            {
                var puuid = participant.GetProperty("puuid").GetString();
                if (!string.Equals(puuid, profile.RiotPuuid, StringComparison.Ordinal)) continue;

                var champId = participant.GetProperty("championId").GetInt32();
                var champName = participant.GetProperty("championName").GetString() ?? string.Empty;
                var kills = participant.GetProperty("kills").GetInt32();
                var deaths = participant.GetProperty("deaths").GetInt32();
                var assists = participant.GetProperty("assists").GetInt32();
                var totalMinions = participant.GetProperty("totalMinionsKilled").GetInt32();
                var neutralMinions = participant.GetProperty("neutralMinionsKilled").GetInt32();
                var cs = totalMinions + neutralMinions;
                var gold = participant.GetProperty("goldEarned").GetInt32();
                var dmg = participant.GetProperty("totalDamageDealtToChampions").GetInt64();
                var vision = participant.GetProperty("visionScore").GetInt32();
                var win = participant.GetProperty("win").GetBoolean();

                var userStats = new UserMatchStatsDto
                {
                    UserId = userId,
                    MatchId = matchId,
                    ChampionId = champId,
                    ChampionName = champName,
                    Kills = kills,
                    Deaths = deaths,
                    Assists = assists,
                    CreepScore = cs,
                    GoldEarned = gold,
                    DamageToChampions = dmg,
                    VisionScore = vision,
                    Win = win,
                    QueueId = queueId,
                    RecordedAtEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };

                await _matchRepo.UpsertUserMatchStatsAsync(userStats);
                break;
            }
        }
    }
}
