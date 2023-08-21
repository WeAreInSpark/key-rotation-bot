using System.Threading.Tasks;

using Kerbee.Graph;

using Microsoft.DurableTask;

namespace Kerbee.Functions;

[DurableTask(nameof(UpdateApplicationsActivity))]
public class UpdateApplicationsActivity : TaskActivity<object, object>
{
    private readonly IApplicationService _applicationService;

    public UpdateApplicationsActivity(IApplicationService applicationService)
    {
        _applicationService = applicationService;
    }

    public async override Task<object> RunAsync(
        TaskActivityContext context,
        object input)
    {
        await _applicationService.UpdateApplications();

        return new object();
    }
}
