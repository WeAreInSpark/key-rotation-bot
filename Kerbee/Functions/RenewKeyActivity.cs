using System;
using System.Threading.Tasks;

using Kerbee.Graph;
using Kerbee.Models;

using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace Kerbee.Functions;

[DurableTask(nameof(RenewKeyActivity))]
public class RenewKeyActivity(
    ILogger<RenewKeyActivity> logger,
    IApplicationService applicationService) : TaskActivity<Application, object>
{
    private readonly ILogger _logger = logger;
    private readonly IApplicationService _applicationService = applicationService;

    public override async Task<object> RunAsync(TaskActivityContext context, Application application)
    {
        try
        {
            await _applicationService.RenewKeyAsync(application);
            return new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error renewing key for application {ApplicationId}", application.Id);
            throw;
        }
    }
}
