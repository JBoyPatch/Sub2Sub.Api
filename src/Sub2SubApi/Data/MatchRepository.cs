using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Sub2SubApi.Application.Models;

namespace Sub2SubApi.Data;

public sealed class MatchRepository
{
    private readonly IAmazonDynamoDB _ddb;
    private readonly string _tableName;

    public MatchRepository(IAmazonDynamoDB ddb, string tableName)
    {
        _ddb = ddb;
        _tableName = tableName;
    }

    private static string MatchPk(string matchId) => $"MATCH#{matchId}";
    private static string MetaSk() => "META";
    private static string UserMatchSk(string matchId) => $"MATCH#{matchId}";
    private static string UserPk(string userId) => $"USER#{userId}";

    public Task<bool> TryUpsertGlobalMatchAsync(MatchDto match)
    {
        // Put if not exists to avoid overwriting raw JSON
        var item = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new() { S = MatchPk(match.MatchId) },
            ["SK"] = new() { S = MetaSk() },
            ["GameStartTimestamp"] = new() { N = match.GameStartTimestamp.ToString() },
            ["QueueId"] = new() { N = match.QueueId.ToString() },
            ["GameDurationSeconds"] = new() { N = match.GameDurationSeconds.ToString() },
            ["CreatedAtEpoch"] = new() { N = match.CreatedAtEpoch.ToString() }
        };

        if (!string.IsNullOrWhiteSpace(match.RawJson))
            item["RawJson"] = new() { S = match.RawJson };

        return _ddb.PutItemAsync(new PutItemRequest
        {
            TableName = _tableName,
            Item = item,
            ConditionExpression = "attribute_not_exists(PK)"
        }).ContinueWith(t =>
        {
            if (t.IsCompletedSuccessfully) return true;
            if (t.Exception?.InnerException is ConditionalCheckFailedException) return false;
            throw t.Exception ?? new Exception("Unknown error");
        });
    }

    public Task UpsertUserMatchStatsAsync(UserMatchStatsDto stats)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new() { S = UserPk(stats.UserId) },
            ["SK"] = new() { S = UserMatchSk(stats.MatchId) },
            ["MatchId"] = new() { S = stats.MatchId },
            ["ChampionId"] = new() { N = stats.ChampionId.ToString() },
            ["ChampionName"] = new() { S = stats.ChampionName },
            ["Kills"] = new() { N = stats.Kills.ToString() },
            ["Deaths"] = new() { N = stats.Deaths.ToString() },
            ["Assists"] = new() { N = stats.Assists.ToString() },
            ["CreepScore"] = new() { N = stats.CreepScore.ToString() },
            ["GoldEarned"] = new() { N = stats.GoldEarned.ToString() },
            ["DamageToChampions"] = new() { N = stats.DamageToChampions.ToString() },
            ["VisionScore"] = new() { N = stats.VisionScore.ToString() },
            ["Win"] = new() { BOOL = stats.Win },
            ["QueueId"] = new() { N = stats.QueueId.ToString() },
            ["RecordedAtEpoch"] = new() { N = stats.RecordedAtEpoch.ToString() }
        };

        return _ddb.PutItemAsync(new PutItemRequest
        {
            TableName = _tableName,
            Item = item
        });
    }

    public async Task<List<UserMatchStatsDto>> ListUserMatchStatsAsync(string userId, int count = 20)
    {
        var resp = await _ddb.QueryAsync(new QueryRequest
        {
            TableName = _tableName,
            KeyConditionExpression = "PK = :pk AND begins_with(SK, :sk)",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":pk"] = new() { S = UserPk(userId) },
                [":sk"] = new() { S = "MATCH#" }
            },
            Limit = count
        });

        var list = new List<UserMatchStatsDto>();
        foreach (var item in resp.Items)
        {
            var matchId = item.TryGetValue("MatchId", out var mid) ? mid.S ?? string.Empty : string.Empty;
            var champId = item.TryGetValue("ChampionId", out var ci) && int.TryParse(ci.N, out var civ) ? civ : 0;
            var champName = item.TryGetValue("ChampionName", out var cn) ? cn.S ?? string.Empty : string.Empty;
            var kills = item.TryGetValue("Kills", out var k) && int.TryParse(k.N, out var kv) ? kv : 0;
            var deaths = item.TryGetValue("Deaths", out var d) && int.TryParse(d.N, out var dv) ? dv : 0;
            var assists = item.TryGetValue("Assists", out var a) && int.TryParse(a.N, out var av) ? av : 0;
            var cs = item.TryGetValue("CreepScore", out var csn) && int.TryParse(csn.N, out var csv) ? csv : 0;
            var gold = item.TryGetValue("GoldEarned", out var g) && int.TryParse(g.N, out var gv) ? gv : 0;
            var dmg = item.TryGetValue("DamageToChampions", out var dm) && long.TryParse(dm.N, out var dmv) ? dmv : 0L;
            var vision = item.TryGetValue("VisionScore", out var vs) && int.TryParse(vs.N, out var vsv) ? vsv : 0;
            var win = item.TryGetValue("Win", out var w) ? w.BOOL : false;
            var queue = item.TryGetValue("QueueId", out var q) && int.TryParse(q.N, out var qv) ? qv : 0;
            var recorded = item.TryGetValue("RecordedAtEpoch", out var r) && long.TryParse(r.N, out var rv) ? rv : 0L;

            list.Add(new UserMatchStatsDto
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
                QueueId = queue,
                RecordedAtEpoch = recorded
            });
        }

        return list;
    }

    public async Task<bool> ExistsUserMatchAsync(string userId, string matchId)
    {
        var resp = await _ddb.GetItemAsync(new GetItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new() { S = UserPk(userId) },
                ["SK"] = new() { S = UserMatchSk(matchId) }
            },
            ProjectionExpression = "MatchId"
        });

        return resp.Item != null && resp.Item.Count > 0;
    }
}
