using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DurableTask.Core;

using Kerbee.Models;
using Kerbee.Options;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kerbee.Functions;

public class RenewCertificatesAndSecrets
{
    private readonly ILogger<RenewCertificatesAndSecrets> _logger;
    private readonly IOptions<KerbeeOptions> _kerbeeOptions;

    public RenewCertificatesAndSecrets(
        ILogger<RenewCertificatesAndSecrets> logger,
        IOptions<KerbeeOptions> kerbeeOptions)
    {
        _logger = logger;
        _kerbeeOptions = kerbeeOptions;
    }

    [Function($"{nameof(RenewCertificatesAndSecrets)}_{nameof(Orchestrator)}")]
    public async Task Orchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        await context.CallUpdateApplicationsActivityAsync(null!);

        var applicationsWithExpiringKeys = await context.CallGetExpiringCertificatesAndSecretsAsync(context.CurrentUtcDateTime.AddDays(_kerbeeOptions.Value.RenewBeforeExpiryInDays));

        foreach (var applicationWithExpiringKey in applicationsWithExpiringKeys.Where(x => x.KeyType == KeyType.None))
        {
            applicationWithExpiringKey.KeyType = _kerbeeOptions.Value.DefaultKeyType;
        }

        var renewalTasks = new List<Task>();
        foreach (var applicationWithExpiringKey in applicationsWithExpiringKeys)
        {
            if (applicationWithExpiringKey.KeyType == KeyType.Certificate)
            {
                renewalTasks.Add(context.CallRenewCertificateActivityAsync(applicationWithExpiringKey));
            }
            else if (applicationWithExpiringKey.KeyType == KeyType.Secret)
            {
                renewalTasks.Add(context.CallRenewSecretActivityAsync(applicationWithExpiringKey));
            }
        }
        await Task.WhenAll(renewalTasks);
    }

    [Function($"{nameof(RenewCertificatesAndSecrets)}_{nameof(Timer)}")]
    public async Task Timer([TimerTrigger("0 0 0 * * *")] TimerInfo timer, [DurableClient] DurableTaskClient starter)
    {
        // Function input comes from the request content.
        var instanceId = await starter.ScheduleNewOrchestrationInstanceAsync($"{nameof(RenewCertificatesAndSecrets)}_{nameof(Orchestrator)}");

        _logger.LogInformation($"Started orchestration with ID = '{instanceId}'.");
    }

    private readonly RetryOptions _retryOptions = new(TimeSpan.FromHours(3), 2)
    {
        Handle = ex => ex.InnerException?.InnerException is RetriableOrchestratorException
    };

    [Function($"{nameof(RenewCertificatesAndSecrets)}_{nameof(HttpStart)}")]
    public async Task<HttpResponseData> HttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/renew")] HttpRequestData req,
        [DurableClient] DurableTaskClient starter)
    {
        // Function input comes from the request content.
        var instanceId = await starter.ScheduleNewOrchestrationInstanceAsync($"{nameof(RenewCertificatesAndSecrets)}_{nameof(Orchestrator)}");

        _logger.LogInformation($"Started orchestration with ID = '{instanceId}'.");

        return starter.CreateCheckStatusResponse(req, instanceId);
    }
}
