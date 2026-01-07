using Amazon.DynamoDBv2;
using Sub2SubApi.Api.Endpoints;
using Sub2SubApi.Application;
using Sub2SubApi.Application.Services;
using Sub2SubApi.Data;
using Sub2SubApi.Infrastructure.Auth;

namespace Sub2SubApi.Api;

public static class ApiBootstrap
{
    public static ApiRouter BuildRouter()
    {
        // Replace with real auth later
        // ICurrentUser currentUser = new DevCurrentUser();

        // DynamoDB client uses Lambda execution role + region from environment by default
        IAmazonDynamoDB ddb = new AmazonDynamoDBClient();

        // Table name from env var (set in Lambda configuration)
        var tableName = Environment.GetEnvironmentVariable("DDB_TABLE_NAME");

        if (string.IsNullOrWhiteSpace(tableName))
            throw new InvalidOperationException("Missing env var DDB_TABLE_NAME.");

        // Repo + service
        var repo = new LobbyRepository(ddb, tableName);
        ILobbyService lobbyService = new LobbyService(repo);

        var router = new ApiRouter();
        LobbyEndpoints.Map(router, lobbyService);

        // Admin endpoints file can exist now but not mapped until need it:
        // AdminLobbyEndpoints.Map(router, ...)

        return router;
    }
}
