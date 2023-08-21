using System.Net;
using System.Threading.Tasks;

using Kerbee.Graph;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Kerbee.Functions;

public class UnmanageApplication
{
    private readonly ILogger _logger;
    private readonly IApplicationService _applicationService;

    public UnmanageApplication(
        IApplicationService applicationService,
        ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<UnmanageApplication>();
        _applicationService = applicationService;
    }

    [Function($"{nameof(UnmanageApplication)}_{nameof(HttpStart)}")]
    public async Task<HttpResponseData> HttpStart([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route =  "api/applications/{applicationId}")] HttpRequestData req,
        string applicationId)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");

        await _applicationService.UnmanageApplicationAsync(applicationId);
        var response = req.CreateResponse(HttpStatusCode.OK);
        return response;
    }
}
