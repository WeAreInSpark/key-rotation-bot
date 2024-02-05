using System.Collections.Generic;
using System.Threading.Tasks;

using Kerbee.Graph;
using Kerbee.Models;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Kerbee.Functions;

public class GetApplicationsActivity(
    ILogger<GetApplicationsActivity> logger,
    IApplicationService applicationService)
{
    private readonly ILogger _logger = logger;
    private readonly IApplicationService _applicationService = applicationService;

    [Function(nameof(GetApplicationsActivity))]
    public async Task<IEnumerable<Application>> RunAsync([ActivityTrigger] object input)
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
