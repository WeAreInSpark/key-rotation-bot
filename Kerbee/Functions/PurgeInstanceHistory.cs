using System;
using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Client;

namespace Kerbee.Functions;

public class PurgeInstanceHistory
{
    [Function($"{nameof(PurgeInstanceHistory)}_{nameof(Timer)}")]
    public Task Timer([TimerTrigger("0 0 0 1 * *")] TimerInfo timer, [DurableClient] DurableTaskClient starter)
    {
        return starter.PurgeInstancesAsync(
            DateTime.MinValue,
            DateTime.UtcNow.AddMonths(-1),
            new[]
            {
                OrchestrationRuntimeStatus.Completed,
                OrchestrationRuntimeStatus.Failed
            });
    }
}
