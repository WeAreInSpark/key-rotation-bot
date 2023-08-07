using System.Threading.Tasks;

using Kerbee.Graph;
using Kerbee.Models;

using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace Kerbee.Functions;

[DurableTask(nameof(RenewCertificateActivity))]
public class RenewCertificateActivity : TaskActivity<Application, object>
{

    private readonly ILogger _logger;
    private readonly IApplicationService _applicationService;

    public RenewCertificateActivity(
        ILogger<RenewCertificateActivity> logger,
        IApplicationService applicationService)
    {
        _logger = logger;
        _applicationService = applicationService;
    }

    public async override Task<object> RunAsync(TaskActivityContext context, Application application)
    {
        await _applicationService.RenewCertificate(application);
        return new();
    }
}
