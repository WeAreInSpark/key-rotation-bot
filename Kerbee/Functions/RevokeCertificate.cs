using System.Net;
using System.Threading.Tasks;

using Kerbee.Internal;

using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace Kerbee.Functions;

public class RevokeCertificate
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private ILogger<RevokeCertificate> _log;

    public RevokeCertificate(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    [Function($"{nameof(RevokeCertificate)}_{nameof(Orchestrator)}")]
    public Task Orchestrator([OrchestrationTrigger] TaskOrchestrationContext context, ILogger<RevokeCertificate> log)
    {
        _log = log;
        var certificateName = context.GetInput<string>();

        // Todo: revoke certificate
        //await activity.RevokeCertificate(certificateName);

        return Task.CompletedTask;
    }

    [Function($"{nameof(RevokeCertificate)}_{nameof(HttpStart)}")]
    public async Task<HttpResponseData> HttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api/certificate/{certificateName}/revoke")] HttpRequestData req,
        string certificateName,
        [DurableClient] DurableTaskClient starter)
    {
        if (!_httpContextAccessor.HttpContext.User.Identity.IsAuthenticated)
        {
            return req.CreateResponse(HttpStatusCode.Unauthorized);
        }

        if (!_httpContextAccessor.HttpContext.User.HasRevokeCertificateRole())
        {
            return req.CreateResponse(HttpStatusCode.Forbidden);
        }

        // Function input comes from the request content.
        var instanceId = await starter.ScheduleNewOrchestrationInstanceAsync($"{nameof(RevokeCertificate)}_{nameof(Orchestrator)}", certificateName);

        _log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

        return starter.CreateCheckStatusResponse(req, instanceId);
    }
}
