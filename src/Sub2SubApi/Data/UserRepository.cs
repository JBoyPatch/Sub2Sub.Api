using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Sub2SubApi.Application.Models;

namespace Sub2SubApi.Data;

public sealed class UserRepository
{
    private readonly IAmazonDynamoDB _ddb;
    private readonly string _tableName;

    public UserRepository(IAmazonDynamoDB ddb, string tableName)
    {
        _ddb = ddb;
        _tableName = tableName;
    }

    private static string Pk(string username) => $"USER#{username}";
    private static string MetaSk() => "META";

    public async Task<UserDto?> GetByUsernameAsync(string username)
    {
        var resp = await _ddb.GetItemAsync(new GetItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new() { S = Pk(username) },
                ["SK"] = new() { S = MetaSk() }
            }
        });

        if (resp.Item == null || resp.Item.Count == 0) return null;

        var id = resp.Item.TryGetValue("Id", out var i) ? i.S : Guid.NewGuid().ToString();
        var email = resp.Item.TryGetValue("Email", out var e) ? e.S : string.Empty;
        var hash = resp.Item.TryGetValue("PasswordHash", out var p) ? p.S : string.Empty;
        var avatar = resp.Item.TryGetValue("AvatarUrl", out var a) ? a.S : null;
        var credits = resp.Item.TryGetValue("Credits", out var c) ? int.Parse(c.N) : 0;
        var type = resp.Item.TryGetValue("Type", out var t) ? t.S : "User";

        return new UserDto
        {
            Id = id ?? Guid.NewGuid().ToString(),
            Username = username,
            Email = email ?? string.Empty,
            PasswordHash = hash ?? string.Empty,
            AvatarUrl = avatar,
            Credits = credits,
            Type = type ?? "User"
        };
    }

    /// <summary>
    /// Creates a user, failing if the user already exists.
    /// Throws ConditionalCheckFailedException if username exists.
    /// </summary>
    public Task CreateUserAsync(UserDto user)
    {
        return _ddb.PutItemAsync(new PutItemRequest
        {
            TableName = _tableName,
            Item = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new() { S = Pk(user.Username) },
                ["SK"] = new() { S = MetaSk() },
                ["Id"] = new() { S = user.Id },
                ["Email"] = new() { S = user.Email },
                ["PasswordHash"] = new() { S = user.PasswordHash },
                ["AvatarUrl"] = new() { S = user.AvatarUrl ?? string.Empty },
                ["Credits"] = new() { N = user.Credits.ToString() },
                ["Type"] = new() { S = user.Type ?? "User" },
                ["CreatedAtEpoch"] = new() { N = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() }
            },
            ConditionExpression = "attribute_not_exists(PK)"
        });
    }

    public async Task<UserDto?> GetByIdAsync(string id)
    {
        // Query the ById GSI for item where attribute Id == id
        var resp = await _ddb.QueryAsync(new QueryRequest
        {
            TableName = _tableName,
            IndexName = "ById",
            KeyConditionExpression = "Id = :id",
            ExpressionAttributeValues = new Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>
            {
                [":id"] = new() { S = id }
            },
            Limit = 1
        });

        if (resp.Items == null || resp.Items.Count == 0) return null;

        var item = resp.Items[0];
        var username = item.TryGetValue("PK", out var pk) ? (pk.S ?? string.Empty).Replace("USER#", string.Empty) : string.Empty;
        var email = item.TryGetValue("Email", out var e) ? e.S : string.Empty;
        var hash = item.TryGetValue("PasswordHash", out var p) ? p.S : string.Empty;
        var avatar = item.TryGetValue("AvatarUrl", out var a) ? a.S : null;
        var credits = item.TryGetValue("Credits", out var c) && int.TryParse(c.N, out var cv) ? cv : 0;
        var type = item.TryGetValue("Type", out var t) ? t.S : "User";

        return new UserDto
        {
            Id = id,
            Username = username ?? string.Empty,
            Email = email ?? string.Empty,
            PasswordHash = hash ?? string.Empty,
            AvatarUrl = avatar,
            Credits = credits,
            Type = type ?? "User"
        };
    }

    public async Task<bool> UpdateAvatarUrlAsync(string id, string? avatarUrl)
    {
        // Use the ById GSI to find the item by Id
        var resp = await _ddb.QueryAsync(new QueryRequest
        {
            TableName = _tableName,
            IndexName = "ById",
            KeyConditionExpression = "Id = :id",
            ExpressionAttributeValues = new Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>
            {
                [":id"] = new() { S = id }
            },
            Limit = 1
        });

        if (resp.Items == null || resp.Items.Count == 0) return false;

        var item = resp.Items[0];
        var pkRaw = item.TryGetValue("PK", out var pk) ? pk.S ?? string.Empty : string.Empty;
        var username = pkRaw.Replace("USER#", string.Empty);

        var key = new Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>
        {
            ["PK"] = new() { S = Pk(username) },
            ["SK"] = new() { S = MetaSk() }
        };

        if (avatarUrl is null)
        {
            await _ddb.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = _tableName,
                Key = key,
                UpdateExpression = "REMOVE AvatarUrl"
            });
        }
        else
        {
            await _ddb.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = _tableName,
                Key = key,
                UpdateExpression = "SET AvatarUrl = :a",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":a"] = new() { S = avatarUrl }
                }
            });
        }

        return true;
    }
}
