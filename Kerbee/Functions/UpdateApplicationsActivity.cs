using System;
using System.Linq;
using System.Threading.Tasks;

using Kerbee.Graph;
using Kerbee.Internal;
using Kerbee.Models;

using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace Kerbee.Functions;

[DurableTask(nameof(UpdateApplicationsActivity))]
public class UpdateApplicationsActivity : TaskActivity<object, object>
{
    private readonly ILogger _logger;
    private readonly IApplicationService _applicationService;
    private readonly IGraphService _graphService;

    public UpdateApplicationsActivity(
        ILogger<UpdateApplicationsActivity> logger,
        IApplicationService applicationService,
        IGraphService graphService)
    {
        _logger = logger;
        _applicationService = applicationService;
        _graphService = graphService;
    }

    public async override Task<object> RunAsync(
        TaskActivityContext context,
        object input)
    {
        

        return new object();
    }
}
