using System.Net;
using System.Threading.Tasks;

using Kerbee.Graph;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Kerbee.Functions
{
    public class RemoveKey
    {
        private readonly ILogger _logger;
        private readonly IApplicationService _applicationService;

        public RemoveKey(
            IApplicationService applicationService,
            ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<RemoveKey>();
            _applicationService = applicationService;
        }

        [Function($"{nameof(RemoveKey)}_{nameof(HttpStart)}")]
        public async Task<HttpResponseData> HttpStart([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            await _applicationService.RemoveKeyAsync(applicationId);
            var response = req.CreateResponse(HttpStatusCode.OK);

            return response;
        }
    }
}
