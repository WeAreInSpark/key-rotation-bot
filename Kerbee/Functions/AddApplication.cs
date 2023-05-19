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

public class AddApplication
{
    private readonly ILogger<AddApplication> _logger;
    private readonly IApplicationService _applicationService;

    public AddApplication(IApplicationService applicationService, ILoggerFactory loggerFactory, IConfiguration configuration)
    {
        _logger = loggerFactory.CreateLogger<AddApplication>();
        _applicationService = applicationService;
    }

    [Function($"{nameof(AddApplication)}_{nameof(HttpStart)}")]
    public async Task<HttpResponseData> HttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api/applications")] HttpRequestData req)
    {
        var application = await req.ReadFromJsonAsync<Application>();

        Guard.IsNotNull(application, nameof(application));

        await _applicationService.AddApplicationAsync(application);

        return req.CreateResponse(HttpStatusCode.OK);
    }
}
