using System;
using System.Threading;
using System.Threading.Tasks;

using DurableTask.Core;

using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace Kerbee.Functions;

public class RenewCertificates
{
    [Function($"{nameof(RenewCertificates)}_{nameof(Orchestrator)}")]
    public async Task Orchestrator([OrchestrationTrigger] TaskOrchestrationContext context, ILogger log)
    {
        var certificates = await context.CallGetExpiringCertificatesAsync(context.CurrentUtcDateTime);

        if (certificates.Count == 0)
        {
            log.LogInformation("Certificates are not found");

            return;
        }

        var jitter = (uint)context.NewGuid().GetHashCode() % 600;

        await context.CreateTimer(context.CurrentUtcDateTime.AddSeconds(jitter), CancellationToken.None);

        foreach (var certificate in certificates)
        {
            log.LogInformation($"{certificate.Id} - {certificate.ExpiresOn}");

            try
            {

            }
            catch (Exception ex)
            {
                log.LogError($"Failed sub orchestration with DNS names = {string.Join(",", certificate.DnsNames)}");
                log.LogError(ex.Message);
            }
        }
    }

    [Function($"{nameof(RenewCertificates)}_{nameof(Timer)}")]
    public async Task Timer([TimerTrigger("0 0 0 * * *")] TimerInfo timer, [DurableClient] DurableTaskClient starter, ILogger log)
    {
        // Function input comes from the request content.
        var instanceId = await starter.ScheduleNewOrchestrationInstanceAsync($"{nameof(RenewCertificates)}_{nameof(Orchestrator)}");

        log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
    }

    private readonly RetryOptions _retryOptions = new(TimeSpan.FromHours(3), 2)
    {
        Handle = ex => ex.InnerException?.InnerException is RetriableOrchestratorException
    };
}
