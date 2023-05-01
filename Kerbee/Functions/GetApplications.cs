using System.Threading.Tasks;

using Kerbee.Graph;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Kerbee.Functions;

public class GetApplications
{
    private readonly ILogger<GetApplications> _logger;
    private readonly IApplicationService _applicationService;

    public GetApplications(IHttpContextAccessor httpContextAccessor, IApplicationService applicationService, ILoggerFactory loggerFactory, IConfiguration configuration)
    {
        _logger = loggerFactory.CreateLogger<GetApplications>();
        _applicationService = applicationService;
    }

    [Function($"{nameof(GetApplications)}_{nameof(HttpStart)}")]
    public async Task<IActionResult> HttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/applications")] HttpRequest req)
    {
        var result = await _applicationService.GetApplicationsAsync();

        return result.Match<IActionResult>(
            apps => new OkObjectResult(apps),
            unauthorized => unauthorized,
            error => throw error.Value
        );
    }
}
