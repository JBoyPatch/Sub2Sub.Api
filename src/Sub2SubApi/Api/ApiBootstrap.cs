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

        // Repo + service for lobbies
        var repo = new LobbyRepository(ddb, tableName);
        ILobbyService lobbyService = new LobbyService(repo);

        // Users table for auth (required)
        var usersTable = Environment.GetEnvironmentVariable("DDB_USERS_TABLE_NAME");
        if (string.IsNullOrWhiteSpace(usersTable))
            throw new InvalidOperationException("Missing env var DDB_USERS_TABLE_NAME.");

        var userRepo = new UserRepository(ddb, usersTable);
        IAuthService authService = new Application.Services.AuthService(userRepo);

        var router = new ApiRouter();
        LobbyEndpoints.Map(router, lobbyService);
        AuthEndpoints.Map(router, authService);

        // Admin endpoints file can exist now but not mapped until need it:
        // AdminLobbyEndpoints.Map(router, ...)

        return router;
    }
}
