using System.Collections.Generic;
using System.Threading.Tasks;

using Kerbee.Models;

using Microsoft.DurableTask;

namespace Kerbee.Functions;

[DurableTask(nameof(GetApplicationsOrchestrator))]
public class GetApplicationsOrchestrator : TaskOrchestrator<object, IEnumerable<Application>>
{
    public async override Task<IEnumerable<Application>> RunAsync(TaskOrchestrationContext context, object input)
    {
        var logger = context.CreateReplaySafeLogger<GetApplicationsOrchestrator>();

        _ = await context.CallUpdateApplicationsActivityAsync(input);
        return await context.CallGetApplicationsActivityAsync(input);
    }
}
