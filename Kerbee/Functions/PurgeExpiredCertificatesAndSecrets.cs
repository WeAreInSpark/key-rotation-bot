using System.Collections.Generic;
using System.Threading.Tasks;

using Kerbee.Models;
using Kerbee.Options;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kerbee.Functions;

public class PurgeExpiredCertificatesAndSecrets
{
    private readonly ILogger<PurgeExpiredCertificatesAndSecrets> _logger;
    private readonly IOptions<KerbeeOptions> _kerbeeOptions;

    public PurgeExpiredCertificatesAndSecrets(
        ILogger<PurgeExpiredCertificatesAndSecrets> logger,
        IOptions<KerbeeOptions> kerbeeOptions)
    {
        _logger = logger;
        _kerbeeOptions = kerbeeOptions;
    }

    [Function($"{nameof(PurgeExpiredCertificatesAndSecrets)}_{nameof(Orchestrator)}")]
    public async Task Orchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        await context.CallActivityAsync(nameof(UpdateApplicationsActivity), new object());

        var applications = await context.CallActivityAsync<IEnumerable<Application>>(nameof(GetApplicationsActivity), new object());

        foreach (var application in applications)
        {
            await context.CallActivityAsync(nameof(PurgeExpiredKeysActivity), application);
        }
    }

    [Function($"{nameof(PurgeExpiredCertificatesAndSecrets)}_{nameof(Timer)}")]
    public async Task Timer([TimerTrigger("0 0 0 * * *")] TimerInfo timer, [DurableClient] DurableTaskClient starter)
    {
        // Function input comes from the request content.
        var instanceId = await starter.ScheduleNewOrchestrationInstanceAsync($"{nameof(PurgeExpiredCertificatesAndSecrets)}_{nameof(Orchestrator)}");

        _logger.LogInformation($"Started orchestration with ID = '{instanceId}'.");
    }

    [Function($"{nameof(PurgeExpiredCertificatesAndSecrets)}_{nameof(HttpStart)}")]
    public async Task<HttpResponseData> HttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/purge")] HttpRequestData req,
        [DurableClient] DurableTaskClient starter)
    {
        // Function input comes from the request content.
        var instanceId = await starter.ScheduleNewOrchestrationInstanceAsync($"{nameof(PurgeExpiredCertificatesAndSecrets)}_{nameof(Orchestrator)}");

        _logger.LogInformation("Started orchestration with {OrchestrationId}", instanceId);

        return starter.CreateCheckStatusResponse(req, instanceId);
    }
}
