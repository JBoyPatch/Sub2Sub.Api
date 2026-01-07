using System.Text.RegularExpressions;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

namespace Sub2SubApi.Api;

public sealed class ApiRouter
{
    private readonly List<Route> _routes = new();

    public void Map(string method, string pattern, Func<RouteContext, Task<APIGatewayProxyResponse>> handler)
    {
        _routes.Add(new Route(method.ToUpperInvariant(), new Regex($"^{pattern}$", RegexOptions.Compiled), handler));
    }

    public async Task<APIGatewayProxyResponse> HandleAsync(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var method = (request.HttpMethod ?? "GET").ToUpperInvariant();
        var path = request.Path ?? "/";

        foreach (var route in _routes)
        {
            if (route.Method != method) continue;

            var match = route.PathRegex.Match(path);
            if (!match.Success) continue;

            var routeParams = match.Groups.Keys
                .Where(k => k != "0")
                .ToDictionary(k => k, k => match.Groups[k].Value);

            var ctx = new RouteContext(request, context, routeParams);
            return await route.Handler(ctx);
        }

        return HttpResults.NotFound(new { message = "Route not found", method, path });
    }

    private sealed record Route(
        string Method,
        Regex PathRegex,
        Func<RouteContext, Task<APIGatewayProxyResponse>> Handler
    );
}

public sealed class RouteContext
{
    public RouteContext(APIGatewayProxyRequest request, ILambdaContext lambda, Dictionary<string, string> routeParams)
    {
        Request = request;
        Lambda = lambda;
        RouteParams = routeParams;
    }

    public APIGatewayProxyRequest Request { get; }
    public ILambdaContext Lambda { get; }
    public Dictionary<string, string> RouteParams { get; }

    public string? GetRouteParam(string key) =>
        RouteParams.TryGetValue(key, out var v) ? v : null;
}
