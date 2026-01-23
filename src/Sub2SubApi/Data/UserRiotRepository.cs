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

        // If not found by PK (some environments store user records by username PK),
        // fall back to scanning for an item with attribute Id == userId.
        if (resp.Item == null || resp.Item.Count == 0)
        {
            var scan = await _ddb.ScanAsync(new Amazon.DynamoDBv2.Model.ScanRequest
            {
                TableName = _tableName,
                Limit = 1,
                FilterExpression = "Id = :id",
                ExpressionAttributeValues = new System.Collections.Generic.Dictionary<string, AttributeValue>
                {
                    [":id"] = new() { S = userId }
                }
            });

            if (scan.Items == null || scan.Items.Count == 0) return null;
            resp.Item = scan.Items[0];
        }

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
        // Use UpdateItem to avoid overwriting other attributes (e.g., password hash) on the same item.
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

        var key = new System.Collections.Generic.Dictionary<string, AttributeValue>
        {
            ["PK"] = new() { S = Pk(profile.UserId) },
            ["SK"] = new() { S = MetaSk() }
        };

        var updateExpr = new System.Text.StringBuilder("SET RiotPuuid = :puuid, RiotGameName = :gn, RiotTagline = :tl, LastRiotProfileSyncAtEpoch = :now");
        var exprVals = new System.Collections.Generic.Dictionary<string, AttributeValue>
        {
            [":puuid"] = new() { S = profile.RiotPuuid ?? string.Empty },
            [":gn"] = new() { S = profile.RiotGameName ?? string.Empty },
            [":tl"] = new() { S = profile.RiotTagline ?? string.Empty },
            [":now"] = new() { N = now }
        };

        if (!string.IsNullOrWhiteSpace(profile.RiotSummonerId))
        {
            updateExpr.Append(", RiotSummonerId = :sid");
            exprVals[":sid"] = new() { S = profile.RiotSummonerId };
        }

        if (profile.RiotProfileIconId.HasValue)
        {
            updateExpr.Append(", RiotProfileIconId = :pi");
            exprVals[":pi"] = new() { N = profile.RiotProfileIconId.Value.ToString() };
        }

        if (profile.RiotSummonerLevel.HasValue)
        {
            updateExpr.Append(", RiotSummonerLevel = :sl");
            exprVals[":sl"] = new() { N = profile.RiotSummonerLevel.Value.ToString() };
        }

        return _ddb.UpdateItemAsync(new UpdateItemRequest
        {
            TableName = _tableName,
            Key = key,
            UpdateExpression = updateExpr.ToString(),
            ExpressionAttributeValues = exprVals
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

    public async Task<List<UserRankedStatsDto>> GetRankedEntriesAsync(string userId)
    {
        var resp = await _ddb.QueryAsync(new QueryRequest
        {
            TableName = _tableName,
            KeyConditionExpression = "PK = :pk AND begins_with(SK, :sk)",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":pk"] = new() { S = Pk(userId) },
                [":sk"] = new() { S = "RANK#" }
            }
        });

        var list = new List<UserRankedStatsDto>();
        foreach (var item in resp.Items)
        {
            var queue = item.TryGetValue("QueueType", out var q) ? q.S ?? string.Empty : string.Empty;
            var tier = item.TryGetValue("Tier", out var t) ? t.S ?? string.Empty : string.Empty;
            var rank = item.TryGetValue("Rank", out var r) ? r.S ?? string.Empty : string.Empty;
            var lp = item.TryGetValue("LeaguePoints", out var lpv) && int.TryParse(lpv.N, out var lpi) ? lpi : 0;
            var wins = item.TryGetValue("Wins", out var w) && int.TryParse(w.N, out var wi) ? wi : 0;
            var losses = item.TryGetValue("Losses", out var l) && int.TryParse(l.N, out var li) ? li : 0;
            var last = item.TryGetValue("LastSyncedAtEpoch", out var ls) && long.TryParse(ls.N, out var lv) ? lv : (long?)null;

            list.Add(new UserRankedStatsDto
            {
                UserId = userId,
                QueueType = queue,
                Tier = tier,
                Rank = rank,
                LeaguePoints = lp,
                Wins = wins,
                Losses = losses,
                LastSyncedAtEpoch = last
            });
        }

        return list;
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

    public async Task<List<UserChampionMasteryDto>> GetChampionMasteriesAsync(string userId)
    {
        var resp = await _ddb.QueryAsync(new QueryRequest
        {
            TableName = _tableName,
            KeyConditionExpression = "PK = :pk AND begins_with(SK, :sk)",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":pk"] = new() { S = Pk(userId) },
                [":sk"] = new() { S = "MASTERY#" }
            }
        });

        var list = new List<UserChampionMasteryDto>();
        foreach (var item in resp.Items)
        {
            var champId = item.TryGetValue("ChampionId", out var ci) && int.TryParse(ci.N, out var cvi) ? cvi : 0;
            var champPoints = item.TryGetValue("ChampionPoints", out var cp) && long.TryParse(cp.N, out var cpv) ? cpv : 0L;
            var champLevel = item.TryGetValue("ChampionLevel", out var cl) && int.TryParse(cl.N, out var clv) ? clv : 0;
            var lastPlay = item.TryGetValue("LastPlayTimeEpoch", out var lp) && long.TryParse(lp.N, out var lpv) ? lpv : (long?)null;
            var chest = item.TryGetValue("ChestGranted", out var ch) ? ch.BOOL : (bool?)null;
            var lastSynced = item.TryGetValue("LastSyncedAtEpoch", out var ls) && long.TryParse(ls.N, out var lsv) ? lsv : (long?)null;

            list.Add(new UserChampionMasteryDto
            {
                UserId = userId,
                ChampionId = champId,
                ChampionPoints = champPoints,
                ChampionLevel = champLevel,
                LastPlayTimeEpoch = lastPlay ?? 0,
                ChestGranted = chest,
                LastSyncedAtEpoch = lastSynced
            });
        }

        return list;
    }
}
