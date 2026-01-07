using Sub2SubApi.Api.Endpoints;
using Sub2SubApi.Application.Services;
using Sub2SubApi.Infrastructure.Auth;

namespace Sub2SubApi.Api;

public static class ApiBootstrap
{
    public static ApiRouter BuildRouter()
    {
        // Replace with real auth later
        ICurrentUser currentUser = new DevCurrentUser();

        // Replace with Dynamo-backed service later
        ILobbyService lobbyService = new LobbyService();

        var router = new ApiRouter();

        LobbyEndpoints.Map(router, lobbyService, currentUser);

        // Admin endpoints file can exist now but not mapped until you need it:
        // AdminLobbyEndpoints.Map(router, ...)

        return router;
    }
}
