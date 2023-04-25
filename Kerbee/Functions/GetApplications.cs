using System.Threading.Tasks;

using Azure.WebJobs.Extensions.HttpApi;

using Kerbee.Graph;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Kerbee.Functions;

public class GetApplications : HttpFunctionBase
{
    private readonly ILogger<GetApplications> _logger;
    private readonly IApplicationService _applicationService;

    public GetApplications(IHttpContextAccessor httpContextAccessor, IApplicationService applicationService, ILoggerFactory loggerFactory, IConfiguration configuration)
        : base(httpContextAccessor)
    {
        _logger = loggerFactory.CreateLogger<GetApplications>();
        _applicationService = applicationService;
    }

    [FunctionName($"{nameof(GetApplications)}_{nameof(HttpStart)}")]
    public async Task<IActionResult> HttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/applications")] HttpRequest req)
    {
        var result = await _applicationService.GetApplicationsAsync();

        return result.Match<IActionResult>(
            apps => new OkObjectResult(apps),
            unauthorized => Unauthorized(),
            error => throw error.Value
        );
    }
}
