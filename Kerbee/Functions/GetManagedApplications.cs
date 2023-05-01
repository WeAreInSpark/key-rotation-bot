using System.Threading.Tasks;

using Kerbee.Graph;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Kerbee.Functions;

public class GetManagedApplications
{
    private readonly ILogger<GetManagedApplications> _logger;
    private readonly IApplicationService _applicationService;

    public GetManagedApplications(IHttpContextAccessor httpContextAccessor, IApplicationService applicationService, ILoggerFactory loggerFactory, IConfiguration configuration)
    {
        _logger = loggerFactory.CreateLogger<GetManagedApplications>();
        _applicationService = applicationService;
    }

    [FunctionName($"{nameof(GetManagedApplications)}_{nameof(HttpStart)}")]
    public async Task<IActionResult> HttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/applications/managed")] HttpRequest req)
    {
        var result = await _applicationService.GetManagedApplicationsAsync();

        return result.Match<IActionResult>(
            apps => new OkObjectResult(apps),
            unauthorized => unauthorized,
            error => throw error.Value
        );
    }
}
