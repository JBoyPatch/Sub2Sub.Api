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

    public sealed record TopBidInfo(
        int Credits,
        string? DisplayName,
        string? AvatarUrl
    );

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

    public Task PutLobbyMetaAsync(string lobbyId, string tournamentName, string startsAtIso, bool active = true) =>
        _ddb.PutItemAsync(new PutItemRequest
        {
            TableName = _tableName,
            Item = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new() { S = Pk(lobbyId) },
                ["SK"] = new() { S = MetaSk() },
                ["TournamentName"] = new() { S = tournamentName },
                ["StartsAtIso"] = new() { S = startsAtIso },
                ["Active"] = new() { BOOL = active }
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

    public async Task<Dictionary<(int TeamIndex, string Role), TopBidInfo>> GetAllTopBidsAsync(string lobbyId)
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

        var dict = new Dictionary<(int, string), TopBidInfo>();

        foreach (var item in resp.Items)
        {
            var sk = item["SK"].S; // TOPBID#0#TOP
            var parts = sk.Split('#');
            if (parts.Length != 3) continue;

            var teamIndex = int.Parse(parts[1]);
            var role = parts[2];

            var credits = item.TryGetValue("TopBidCredits", out var b) ? int.Parse(b.N) : 0;
            var displayName = item.TryGetValue("TopBidderDisplayName", out var dn) ? dn.S : null;
            var avatarUrl = item.TryGetValue("TopBidderAvatarUrl", out var au) ? au.S : null;

            dict[(teamIndex, role)] = new TopBidInfo(credits, displayName, avatarUrl);
        }

        return dict;
    }

    /// <summary>
    /// Return all lobby ids that have a META item.
    /// This performs a Scan with a filter; acceptable for low-volume lists.
    /// </summary>
    public async Task<string[]> GetAllLobbyIdsAsync()
    {
        var resp = await _ddb.ScanAsync(new ScanRequest
        {
            TableName = _tableName,
            FilterExpression = "SK = :meta AND begins_with(PK, :prefix) AND Active = :active",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":meta"] = new() { S = MetaSk() },
                [":prefix"] = new() { S = "LOBBY#" },
                [":active"] = new() { BOOL = true }
            },
            ProjectionExpression = "PK"
        });

        var ids = resp.Items
            .Where(i => i.TryGetValue("PK", out var pk) && pk.S is not null)
            .Select(i => i["PK"].S!)
            .Select(pk => pk.StartsWith("LOBBY#") ? pk.Substring("LOBBY#".Length) : pk)
            .ToArray();

        return ids;
    }

    /// <summary>
    /// Atomic "only if higher" update. Returns (accepted, newTopBid).
    /// </summary>
    public async Task<(bool Accepted, int CurrentTopBid)> TryPlaceTopBidAsync(
        string lobbyId,
        int teamIndex,
        string role,
        int amount,
        string bidderUserId,
        string bidderDisplayName,
        string? bidderAvatarUrl)
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
                UpdateExpression =
                    "SET TopBidCredits = :b, TopBidderUserId = :u, TopBidderDisplayName = :dn, UpdatedAtEpoch = :t" +
                    (string.IsNullOrWhiteSpace(bidderAvatarUrl) ? " REMOVE TopBidderAvatarUrl" : ", TopBidderAvatarUrl = :au"),
                ConditionExpression = "attribute_not_exists(TopBidCredits) OR TopBidCredits < :b",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":b"] = new() { N = amount.ToString() },
                    [":u"] = new() { S = bidderUserId },
                    [":dn"] = new() { S = bidderDisplayName },
                    [":t"] = new() { N = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() },
                    // only used when avatarUrl is provided
                    [":au"] = new() { S = bidderAvatarUrl ?? "" }
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
