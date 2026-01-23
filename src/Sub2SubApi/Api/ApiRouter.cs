using System.Text.RegularExpressions;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

namespace Sub2SubApi.Api;

public sealed class ApiRouter
{
    private readonly List<Route> _routes = new();

    public void Map(
        string method,
        string pattern,
        Func<RouteContext, Task<APIGatewayHttpApiV2ProxyResponse>> handler)
    {
        _routes.Add(new Route(
            method.ToUpperInvariant(),
            new Regex($"^{pattern}$", RegexOptions.Compiled),
            handler));
    }

    public async Task<APIGatewayHttpApiV2ProxyResponse> HandleAsync(
        APIGatewayHttpApiV2ProxyRequest request,
        ILambdaContext context)
    {
        var method = (request.RequestContext?.Http?.Method ?? "GET").ToUpperInvariant();
        var path = request.RawPath ?? "/";

        // API Gateway HTTP APIs sometimes include the stage in RawPath (eg "/$default/..."),
        // and some local dev proxies add an "/api" prefix (eg "/api/$default/...").
        // Strip these common prefixes so routes registered without the stage still match.
        if (path.StartsWith("/api/$default/", StringComparison.Ordinal))
            path = path.Substring("/api/$default".Length);
        else if (path.StartsWith("/$default/", StringComparison.Ordinal))
            path = path.Substring("/$default".Length);
        else if (string.Equals(path, "/*$default", StringComparison.Ordinal))
            path = "/";

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
        Func<RouteContext, Task<APIGatewayHttpApiV2ProxyResponse>> Handler
    );
}

public sealed class RouteContext
{
    public RouteContext(
        APIGatewayHttpApiV2ProxyRequest request,
        ILambdaContext lambda,
        Dictionary<string, string> routeParams)
    {
        Request = request;
        Lambda = lambda;
        RouteParams = routeParams;
    }

    public APIGatewayHttpApiV2ProxyRequest Request { get; }
    public ILambdaContext Lambda { get; }
    public Dictionary<string, string> RouteParams { get; }

    public string? GetRouteParam(string key) =>
        RouteParams.TryGetValue(key, out var v) ? v : null;
}
