using System;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Sub2SubApi.Application.Models;

namespace Sub2SubApi.Data;

public sealed class UserRiotRepository
{
    private readonly IAmazonDynamoDB _ddb;
    private readonly string _tableName;

    public UserRiotRepository(IAmazonDynamoDB ddb, string tableName)
    {
        _ddb = ddb;
        _tableName = tableName;
    }

    private static string Pk(string userId) => $"USER#{userId}";
    private static string MetaSk() => "META";

    public async Task<RiotProfileDto?> GetRiotProfileAsync(string userId)
    {
        var resp = await _ddb.GetItemAsync(new GetItemRequest
        {
            TableName = _tableName,
            Key = new System.Collections.Generic.Dictionary<string, AttributeValue>
            {
                ["PK"] = new() { S = Pk(userId) },
                ["SK"] = new() { S = MetaSk() }
            }
        });

        if (resp.Item == null || resp.Item.Count == 0) return null;

        var puuid = resp.Item.TryGetValue("RiotPuuid", out var r) ? r.S : null;
        if (string.IsNullOrWhiteSpace(puuid)) return null;

        return new RiotProfileDto
        {
            UserId = userId,
            RiotPuuid = puuid ?? string.Empty,
            RiotGameName = resp.Item.TryGetValue("RiotGameName", out var gn) ? gn.S ?? string.Empty : string.Empty,
            RiotTagline = resp.Item.TryGetValue("RiotTagline", out var tl) ? tl.S ?? string.Empty : string.Empty,
            RiotSummonerId = resp.Item.TryGetValue("RiotSummonerId", out var sid) ? sid.S : null,
            RiotProfileIconId = resp.Item.TryGetValue("RiotProfileIconId", out var pi) && int.TryParse(pi.N, out var iv) ? iv : null,
            RiotSummonerLevel = resp.Item.TryGetValue("RiotSummonerLevel", out var sl) && long.TryParse(sl.N, out var lv) ? lv : null,
            LastRiotProfileSyncAtEpoch = resp.Item.TryGetValue("LastRiotProfileSyncAtEpoch", out var ls) && long.TryParse(ls.N, out var le) ? le : null
        };
    }

    public Task UpsertRiotProfileAsync(RiotProfileDto profile)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var item = new System.Collections.Generic.Dictionary<string, AttributeValue>
        {
            ["PK"] = new() { S = Pk(profile.UserId) },
            ["SK"] = new() { S = MetaSk() },
            ["RiotPuuid"] = new() { S = profile.RiotPuuid },
            ["RiotGameName"] = new() { S = profile.RiotGameName },
            ["RiotTagline"] = new() { S = profile.RiotTagline },
            ["LastRiotProfileSyncAtEpoch"] = new() { N = now }
        };

        if (!string.IsNullOrWhiteSpace(profile.RiotSummonerId))
            item["RiotSummonerId"] = new() { S = profile.RiotSummonerId };
        if (profile.RiotProfileIconId.HasValue)
            item["RiotProfileIconId"] = new() { N = profile.RiotProfileIconId.Value.ToString() };
        if (profile.RiotSummonerLevel.HasValue)
            item["RiotSummonerLevel"] = new() { N = profile.RiotSummonerLevel.Value.ToString() };

        return _ddb.PutItemAsync(new PutItemRequest
        {
            TableName = _tableName,
            Item = item
        });
    }

    public Task UpsertRankedEntryAsync(UserRankedStatsDto ranked)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new() { S = Pk(ranked.UserId) },
            ["SK"] = new() { S = $"RANK#{ranked.QueueType}" },
            ["QueueType"] = new() { S = ranked.QueueType },
            ["Tier"] = new() { S = ranked.Tier },
            ["Rank"] = new() { S = ranked.Rank },
            ["LeaguePoints"] = new() { N = ranked.LeaguePoints.ToString() },
            ["Wins"] = new() { N = ranked.Wins.ToString() },
            ["Losses"] = new() { N = ranked.Losses.ToString() },
            ["LastSyncedAtEpoch"] = new() { N = (ranked.LastSyncedAtEpoch ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds()).ToString() }
        };

        return _ddb.PutItemAsync(new PutItemRequest
        {
            TableName = _tableName,
            Item = item
        });
    }

    public Task UpsertChampionMasteryAsync(UserChampionMasteryDto mastery)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new() { S = Pk(mastery.UserId) },
            ["SK"] = new() { S = $"MASTERY#{mastery.ChampionId}" },
            ["ChampionId"] = new() { N = mastery.ChampionId.ToString() },
            ["ChampionPoints"] = new() { N = mastery.ChampionPoints.ToString() },
            ["ChampionLevel"] = new() { N = mastery.ChampionLevel.ToString() },
            ["LastPlayTimeEpoch"] = new() { N = (mastery.LastPlayTimeEpoch ?? 0).ToString() },
            ["ChestGranted"] = new() { BOOL = mastery.ChestGranted ?? false },
            ["LastSyncedAtEpoch"] = new() { N = (mastery.LastSyncedAtEpoch ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds()).ToString() }
        };

        return _ddb.PutItemAsync(new PutItemRequest
        {
            TableName = _tableName,
            Item = item
        });
    }
}
