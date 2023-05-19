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

public class GetUnmanagedApplications
{
    private readonly ILogger<GetUnmanagedApplications> _logger;
    private readonly IGraphService _graphService;

    public GetUnmanagedApplications(
        IGraphService graphService,
        ILoggerFactory loggerFactory,
        IConfiguration configuration)
    {
        _logger = loggerFactory.CreateLogger<GetUnmanagedApplications>();
        _graphService = graphService;
    }

    [Function($"{nameof(GetUnmanagedApplications)}_{nameof(HttpStart)}")]
    public async Task<HttpResponseData> HttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/applications/unmanaged")] HttpRequestData req)
    {
        var applications = await _graphService.GetUnmanagedApplicationsAsync();

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(applications.ToModel());
        return response;
    }
}
