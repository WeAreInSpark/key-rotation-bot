using System;
using System.Threading.Tasks;

using Kerbee.Graph;
using Kerbee.Models;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Kerbee.Functions;

public class PurgeExpiredKeysActivity(
    ILogger<PurgeExpiredKeysActivity> logger,
    IApplicationService applicationService)
{
    private readonly ILogger _logger = logger;
    private readonly IApplicationService _applicationService = applicationService;

    [Function(nameof(PurgeExpiredKeysActivity))]
    public async Task<object> RunAsync([ActivityTrigger] Application application)
    {
        try
        {
            await _applicationService.PurgeKeys(application);
            return new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error purging keys for application {ApplicationId}", application.Id);
            throw;
        }
    }
}
