using System;
using System.Net;
using System.Threading.Tasks;

using Kerbee.Internal;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Kerbee.Functions;

public class StaticPage
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public StaticPage(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    [Function($"{nameof(StaticPage)}_{nameof(Serve)}")]
    public async Task<HttpResponseData> Serve(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{*path}")] HttpRequestData req,
        ILogger log)
    {
        if (!IsEasyAuthEnabled || !_httpContextAccessor.HttpContext.User.Identity.IsAuthenticated)
        {
            return req.CreateResponse(HttpStatusCode.Unauthorized);
        }

        return await req.CreateStaticAppResponse();
    }

    private static bool IsEasyAuthEnabled => bool.TryParse(Environment.GetEnvironmentVariable("WEBSITE_AUTH_ENABLED"), out var result) && result;
}
