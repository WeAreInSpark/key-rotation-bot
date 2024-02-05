using System;
using System.Threading.Tasks;

using Kerbee.Graph;

using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace Kerbee.Functions;

public class UpdateApplicationsActivity(
    IApplicationService applicationService,
    ILogger<UpdateApplicationsActivity> logger)
{
    private readonly IApplicationService _applicationService = applicationService;
    private readonly ILogger<UpdateApplicationsActivity> _logger = logger;

    [Function(nameof(UpdateApplicationsActivity))]
    public async Task<object> RunAsync([ActivityTrigger] object input)
    {
        try
        {
            await _applicationService.UpdateApplications();
            return new object();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating applications");
            throw;
        }
    }
}
