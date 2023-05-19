using System.Net;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;

namespace Kerbee.Functions;

public class GetInstanceState
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetInstanceState(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    [Function($"{nameof(GetInstanceState)}_{nameof(HttpStart)}")]
    public async Task<HttpResponseData> HttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/state/{instanceId}")] HttpRequestData req,
        string instanceId,
        [DurableClient] DurableTaskClient starter)
    {
        if (!_httpContextAccessor.HttpContext.User.Identity.IsAuthenticated)
        {
            return req.CreateResponse(HttpStatusCode.Unauthorized);
        }

        var instance = await starter.GetInstanceAsync(instanceId);

        if (instance is null)
        {
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        if (instance.RuntimeStatus == OrchestrationRuntimeStatus.Failed)
        {
            return req.CreateResponse(HttpStatusCode.InternalServerError);
        }

        if (instance.RuntimeStatus is OrchestrationRuntimeStatus.Running or OrchestrationRuntimeStatus.Pending)
        {
            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        return req.CreateResponse(HttpStatusCode.OK);
    }
}
