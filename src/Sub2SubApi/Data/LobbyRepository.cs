using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace Sub2SubApi.Data;

public sealed class LobbyRepository 
{
    private readonly IAmazonDynamoDB _ddb;
    private readonly string _tableName;

    public LobbyRepository(IAmazonDynamoDB ddb, string tableName)
    {
        _ddb = ddb;
        _tableName = tableName;
    }

    private static string Pk(string lobbyId) => $"LOBBY#{lobbyId}";
    private static string MetaSk() => "META";
    private static string TopBidSk(int teamIndex, string role) => $"TOPBID#{teamIndex}#{role.ToUpperInvariant()}";

    public async Task<(string TournamentName, string StartsAtIso)?> GetLobbyMetaAsync(string lobbyId)
    {
        var resp = await _ddb.GetItemAsync(new GetItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new() { S = Pk(lobbyId) },
                ["SK"] = new() { S = MetaSk() }
            }
        });

        if (resp.Item == null || resp.Item.Count == 0) return null;

        var name = resp.Item.TryGetValue("TournamentName", out var n) ? n.S : "Bronze War";
        var starts = resp.Item.TryGetValue("StartsAtIso", out var s) ? s.S : DateTimeOffset.UtcNow.AddMinutes(5).ToString("O");

        return (name, starts);
    }

    public Task PutLobbyMetaAsync(string lobbyId, string tournamentName, string startsAtIso) =>
        _ddb.PutItemAsync(new PutItemRequest
        {
            TableName = _tableName,
            Item = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new() { S = Pk(lobbyId) },
                ["SK"] = new() { S = MetaSk() },
                ["TournamentName"] = new() { S = tournamentName },
                ["StartsAtIso"] = new() { S = startsAtIso }
            }
        });

    public async Task<int> GetTopBidAsync(string lobbyId, int teamIndex, string role)
    {
        var resp = await _ddb.GetItemAsync(new GetItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new() { S = Pk(lobbyId) },
                ["SK"] = new() { S = TopBidSk(teamIndex, role) }
            }
        });

        if (resp.Item == null || resp.Item.Count == 0) return 0;
        return resp.Item.TryGetValue("TopBidCredits", out var v) ? int.Parse(v.N) : 0;
    }

    public async Task<Dictionary<(int TeamIndex, string Role), int>> GetAllTopBidsAsync(string lobbyId)
    {
        var resp = await _ddb.QueryAsync(new QueryRequest
        {
            TableName = _tableName,
            KeyConditionExpression = "PK = :pk AND begins_with(SK, :prefix)",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":pk"] = new() { S = Pk(lobbyId) },
                [":prefix"] = new() { S = "TOPBID#" }
            }
        });

        var dict = new Dictionary<(int, string), int>();

        foreach (var item in resp.Items)
        {
            var sk = item["SK"].S; // TOPBID#0#TOP
            var parts = sk.Split('#');
            if (parts.Length != 3) continue;

            var teamIndex = int.Parse(parts[1]);
            var role = parts[2];

            var bid = item.TryGetValue("TopBidCredits", out var b) ? int.Parse(b.N) : 0;
            dict[(teamIndex, role)] = bid;
        }

        return dict;
    }

    /// <summary>
    /// Atomic "only if higher" update. Returns (accepted, newTopBid).
    /// </summary>
    public async Task<(bool Accepted, int CurrentTopBid)> TryPlaceTopBidAsync(
        string lobbyId,
        int teamIndex,
        string role,
        int amount,
        string bidderUserId)
    {
        role = role.ToUpperInvariant();

        try
        {
            await _ddb.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["PK"] = new() { S = Pk(lobbyId) },
                    ["SK"] = new() { S = TopBidSk(teamIndex, role) }
                },
                UpdateExpression = "SET TopBidCredits = :b, TopBidderUserId = :u, UpdatedAtEpoch = :t",
                ConditionExpression = "attribute_not_exists(TopBidCredits) OR TopBidCredits < :b",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":b"] = new() { N = amount.ToString() },
                    [":u"] = new() { S = bidderUserId },
                    [":t"] = new() { N = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() }
                }
            });

            return (true, amount);
        }
        catch (ConditionalCheckFailedException)
        {
            var current = await GetTopBidAsync(lobbyId, teamIndex, role);
            return (false, current);
        }
    }
}
