using System;
using System.Net;
using System.Threading.Tasks;

using Kerbee.Internal;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Kerbee.Functions;

public class StaticPage
{
    private readonly IClaimsPrincipalAccessor _claimsPrincipalAccessor;

    public StaticPage(IClaimsPrincipalAccessor claimsPrincipalAccessor)
    {
        _claimsPrincipalAccessor = claimsPrincipalAccessor;
    }

    [Function($"{nameof(StaticPage)}_{nameof(Serve)}")]
    public async Task<HttpResponseData> Serve(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{*path}")] HttpRequestData req)
    {
        if (!IsEasyAuthEnabled || _claimsPrincipalAccessor.Principal?.Identity?.IsAuthenticated != true)
        {
            return req.CreateResponse(HttpStatusCode.Unauthorized);
        }

        return await req.CreateStaticAppResponse();
    }

    private static bool IsEasyAuthEnabled => bool.TryParse(Environment.GetEnvironmentVariable("WEBSITE_AUTH_ENABLED"), out var result) && result;
}
