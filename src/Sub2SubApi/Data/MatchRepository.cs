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
