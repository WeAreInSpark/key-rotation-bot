using System.Collections.Generic;
using System.Threading.Tasks;

using Kerbee.Graph;
using Kerbee.Models;

using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace Kerbee.Functions;

[DurableTask(nameof(GetApplicationsActivity))]
public class GetApplicationsActivity(
    ILogger<GetApplicationsActivity> logger,
    IApplicationService applicationService) : TaskActivity<object, IEnumerable<Application>>
{
    private readonly ILogger _logger = logger;
    private readonly IApplicationService _applicationService = applicationService;

    public override async Task<IEnumerable<Application>> RunAsync(TaskActivityContext context, object input)
    {
        try
        {
            return await _applicationService.GetApplicationsAsync();
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error getting applications");
            throw;
        }
    }
}
