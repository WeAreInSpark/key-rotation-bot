using System.Net;

using Kerbee.Graph;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Kerbee.Functions;

public class RenewKey
{
    private readonly ILogger _logger;
    private readonly IApplicationService _applicationService;

    public RenewKey(
        IApplicationService applicationService,
        ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<RenewKey>();
        _applicationService = applicationService;
    }

    [Function($"{nameof(RenewKey)}_{nameof(HttpStart)}")]
    public HttpResponseData HttpStart([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api/applications/{applicationId}/renew")] HttpRequestData req,
        string applicationId)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");

        _applicationService.RenewKeyAsync(applicationId);
        var response = req.CreateResponse(HttpStatusCode.OK);

        return response;
    }
}
