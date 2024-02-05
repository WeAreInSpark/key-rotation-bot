using System;
using System.Threading.Tasks;

using Kerbee.Graph;
using Kerbee.Models;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Kerbee.Functions;

public class RenewKeyActivity(
    ILogger<RenewKeyActivity> logger,
    IApplicationService applicationService)
{
    private readonly ILogger _logger = logger;
    private readonly IApplicationService _applicationService = applicationService;

    [Function(nameof(RenewKeyActivity))]
    public async Task<object> RunAsync([ActivityTrigger] Application application)
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
