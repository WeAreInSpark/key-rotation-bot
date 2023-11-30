using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kerbee.Internal;

public class ClaimsPrincipalMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger<ClaimsPrincipalMiddleware> _logger;

    public ClaimsPrincipalMiddleware(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<ClaimsPrincipalMiddleware>();
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        // determine the type, the default is Microsoft.Azure.Functions.Worker.Context.Features.GrpcFunctionBindingsFeature
        (var featureType, var featureInstance) = context.Features.SingleOrDefault(x => x.Key.Name == "IFunctionBindingsFeature");

        // find the input binding of the function which has been invoked and then find the associated parameter of the function for the data we want
        var inputData = featureType.GetProperties().SingleOrDefault(p => p.Name == "InputData")?.GetValue(featureInstance) as IReadOnlyDictionary<string, object>;
        var requestData = inputData?.Values.SingleOrDefault(obj => obj is HttpRequestData) as HttpRequestData;

        var accessor = context.InstanceServices.GetRequiredService<IClaimsPrincipalAccessor>();

        _logger.LogInformation("Try to find x-ms-token-aad-access-token header");
        if (requestData?.Headers.TryGetValues("x-ms-token-aad-access-token", out var accessTokenHeader) == true)
        {
            // set the access token on the accessor from DI
            accessor.AccessToken = accessTokenHeader.FirstOrDefault();
            _logger.LogInformation("Found x-ms-token-aad-access-token header");
        }

        if (requestData?.Headers.TryGetValues("Authorization", out var authorizationHeader) == true)
        {
            // set the access token on the accessor from DI
            accessor.AccessToken = authorizationHeader.FirstOrDefault()?.ToString().Substring("Bearer ".Length);
            _logger.LogInformation("Found Authorization header");
        }

        if (requestData?.ParsePrincipal() is ClaimsPrincipal principal)
        {
            // set the principal on the accessor from DI
            accessor.Principal = principal;
        }

        await next(context);
    }
}
