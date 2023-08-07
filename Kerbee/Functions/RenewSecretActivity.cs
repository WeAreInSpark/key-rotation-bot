using System.Threading.Tasks;

using Kerbee.Graph;
using Kerbee.Models;

using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace Kerbee.Functions;

[DurableTask(nameof(RenewSecretActivity))]
public class RenewSecretActivity : TaskActivity<Application, object>
{

    private readonly ILogger _logger;
    private readonly IApplicationService _applicationService;

    public RenewSecretActivity(
        ILogger<RenewSecretActivity> logger,
        IApplicationService applicationService)
    {
        _logger = logger;
        _applicationService = applicationService;
    }

    public async override Task<object> RunAsync(TaskActivityContext context, Application application)
    {
        await _applicationService.RenewSecret(application);
        return new();
    }
}
