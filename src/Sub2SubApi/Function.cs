using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Sub2SubApi.Api;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Sub2SubApi;

public class Function
{
    private static readonly ApiRouter _router = ApiBootstrap.BuildRouter();

    public async Task<APIGatewayHttpApiV2ProxyResponse> FunctionHandler(
        APIGatewayHttpApiV2ProxyRequest request,
        ILambdaContext context)
    {
        return await _router.HandleAsync(request, context);
    }
}
