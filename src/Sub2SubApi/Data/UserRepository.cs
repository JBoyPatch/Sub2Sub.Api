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

        return new UserDto
        {
            Id = id ?? Guid.NewGuid().ToString(),
            Username = username,
            Email = email ?? string.Empty,
            PasswordHash = hash ?? string.Empty,
            AvatarUrl = avatar,
            Credits = credits
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
                ["CreatedAtEpoch"] = new() { N = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() }
            },
            ConditionExpression = "attribute_not_exists(PK)"
        });
    }
}
