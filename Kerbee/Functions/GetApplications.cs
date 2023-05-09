using System.Net;
using System.Threading.Tasks;

using Kerbee.Graph;
using Kerbee.Models;

using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Kerbee.Functions;

public class GetApplications
{
    private readonly ILogger<GetApplications> _logger;
    private readonly IApplicationService _applicationService;

    public GetApplications(IApplicationService applicationService, ILoggerFactory loggerFactory, IConfiguration configuration)
    {
        _logger = loggerFactory.CreateLogger<GetApplications>();
        _applicationService = applicationService;
    }

    [Function($"{nameof(GetApplications)}_{nameof(HttpStart)}")]
    public async Task<HttpResponseData> HttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/applications")] HttpRequestData req)
    {
        var result = await _applicationService.GetApplicationsAsync();

        var task = result.Match(
            apps =>
                Task.Run(async () =>
                {
                    var response = req.CreateResponse(HttpStatusCode.OK);
                    await response.WriteAsJsonAsync(apps);
                    return response;
                }),
            unauthorized =>
                Task.FromResult(req.CreateResponse(HttpStatusCode.Unauthorized)),
            error =>
                throw error.Value
        );

        return await task;
    }
}
