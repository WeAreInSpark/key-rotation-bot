using System.Collections.Generic;
using System.Threading.Tasks;

using Kerbee.Graph;
using Kerbee.Models;

using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace Kerbee.Functions;

[DurableTask(nameof(GetApplicationsActivity))]
public class GetApplicationsActivity : TaskActivity<object, IEnumerable<Application>>
{
    private readonly ILogger _logger;
    private readonly IApplicationService _applicationService;

    public GetApplicationsActivity(
        ILogger<GetApplicationsActivity> logger,
        IApplicationService applicationService)
    {
        _logger = logger;
        _applicationService = applicationService;
    }

    public async override Task<IEnumerable<Application>> RunAsync(TaskActivityContext context, object input)
    {
        return await _applicationService.GetApplicationsAsync();
    }
}
