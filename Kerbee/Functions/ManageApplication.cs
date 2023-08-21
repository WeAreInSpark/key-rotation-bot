using System.Net;
using System.Threading.Tasks;

using CommunityToolkit.Diagnostics;

using Kerbee.Graph;
using Kerbee.Models;

using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Kerbee.Functions;

public class ManageApplication
{
    private readonly ILogger<ManageApplication> _logger;
    private readonly IApplicationService _applicationService;

    public ManageApplication(IApplicationService applicationService, ILoggerFactory loggerFactory, IConfiguration configuration)
    {
        _logger = loggerFactory.CreateLogger<ManageApplication>();
        _applicationService = applicationService;
    }

    [Function($"{nameof(ManageApplication)}_{nameof(HttpStart)}")]
    public async Task<HttpResponseData> HttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api/applications")] HttpRequestData req)
    {
        var application = await req.ReadFromJsonAsync<Application>();

        Guard.IsNotNull(application, nameof(application));

        await _applicationService.AddApplicationAsync(application, true);
        await _applicationService.RenewKeyAsync(application);

        return req.CreateResponse(HttpStatusCode.OK);
    }
}
