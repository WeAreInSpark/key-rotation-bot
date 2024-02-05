using System;
using System.Threading.Tasks;

using Kerbee.Graph;

using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace Kerbee.Functions;

[DurableTask(nameof(UpdateApplicationsActivity))]
public class UpdateApplicationsActivity(
    IApplicationService applicationService,
    ILogger<UpdateApplicationsActivity> logger) : TaskActivity<object, object>
{
    private readonly IApplicationService _applicationService = applicationService;
    private readonly ILogger<UpdateApplicationsActivity> _logger = logger;

    public override async Task<object> RunAsync(
        TaskActivityContext context,
        object input)
    {
        try
        {
            _logger.LogInformation("Updating applications");
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
