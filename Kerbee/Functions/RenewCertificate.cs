using System.Net;

using Kerbee.Internal;

using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace Kerbee.Functions;

public class RenewCertificate
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RenewCertificate(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    [Function($"{nameof(RenewCertificate)}_{nameof(HttpStart)}")]
    public HttpResponseData HttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api/certificate/{certificateName}/renew")] HttpRequestData req,
        string certificateName,
        [DurableClient] DurableTaskClient starter,
        ILogger log)
    {
        if (!_httpContextAccessor.HttpContext.User.Identity.IsAuthenticated)
        {
            return req.CreateResponse(HttpStatusCode.Unauthorized);
        }

        if (!_httpContextAccessor.HttpContext.User.HasIssueCertificateRole())
        {
            return req.CreateResponse(HttpStatusCode.Forbidden);
        }

        return req.CreateResponse(HttpStatusCode.OK);
    }
}
