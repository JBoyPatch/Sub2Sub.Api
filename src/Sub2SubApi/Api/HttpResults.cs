using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;

namespace Sub2SubApi.Api;

public static class HttpResults
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static APIGatewayProxyResponse Ok(object body) =>
        Json(200, body);

    public static APIGatewayProxyResponse BadRequest(object body) =>
        Json(400, body);

    public static APIGatewayProxyResponse NotFound(object body) =>
        Json(404, body);

    public static APIGatewayProxyResponse Json(int statusCode, object body)
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = statusCode,
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json",
                ["Access-Control-Allow-Origin"] = "*",
                ["Access-Control-Allow-Headers"] = "*",
                ["Access-Control-Allow-Methods"] = "GET,POST,OPTIONS"
            },
            Body = JsonSerializer.Serialize(body, JsonOptions)
        };
    }
}
