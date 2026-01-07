using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;

namespace Sub2SubApi.Api;

public static class HttpResults
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static APIGatewayHttpApiV2ProxyResponse Ok(object body) =>
        Json(200, body);

    public static APIGatewayHttpApiV2ProxyResponse BadRequest(object body) =>
        Json(400, body);

    public static APIGatewayHttpApiV2ProxyResponse NotFound(object body) =>
        Json(404, body);

    public static APIGatewayHttpApiV2ProxyResponse Json(int statusCode, object body)
    {
        return new APIGatewayHttpApiV2ProxyResponse
        {
            StatusCode = statusCode,
            Headers = new Dictionary<string, string>
            {
                // Header names are case-insensitive, but lowercase is common in Lambda/APIGW examples
                ["content-type"] = "application/json",
                ["access-control-allow-origin"] = "*",
                ["access-control-allow-headers"] = "*",
                ["access-control-allow-methods"] = "GET,POST,OPTIONS"
            },
            Body = JsonSerializer.Serialize(body, JsonOptions)
        };
    }
}
