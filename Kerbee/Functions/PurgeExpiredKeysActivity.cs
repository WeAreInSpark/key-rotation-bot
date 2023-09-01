using System.Threading.Tasks;

using Kerbee.Graph;
using Kerbee.Models;

using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace Kerbee.Functions;

[DurableTask(nameof(PurgeExpiredKeysActivity))]
public class PurgeExpiredKeysActivity : TaskActivity<Application, object>
{
    private readonly ILogger _logger;
    private readonly IApplicationService _applicationService;

    public PurgeExpiredKeysActivity(
        ILogger<PurgeExpiredKeysActivity> logger,
        IApplicationService applicationService)
    {
        _logger = logger;
        _applicationService = applicationService;
    }

    public async override Task<object> RunAsync(TaskActivityContext context, Application application)
    {
        await _applicationService.PurgeKeys(application);
        return new();
    }
}
