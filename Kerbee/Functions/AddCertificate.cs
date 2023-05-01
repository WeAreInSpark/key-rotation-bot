using System.Net;
using System.Threading.Tasks;

using Kerbee.Internal;
using Kerbee.Models;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace Kerbee.Functions;

public class AddCertificate
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AddCertificate(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    [Function($"{nameof(AddCertificate)}_{nameof(HttpStart)}")]
    public async Task<HttpResponseData> HttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api/certificate")] HttpRequestData req,
        [DurableClient] DurableTaskClient starter,
        ILogger log)
    {
        if (!_httpContextAccessor.HttpContext.User.Identity.IsAuthenticated)
        {
            return req.CreateResponse(HttpStatusCode.Unauthorized);
        }

        if (!!_httpContextAccessor.HttpContext.User.HasIssueCertificateRole())
        {
            return req.CreateResponse(HttpStatusCode.Forbidden);
        }

        var certificatePolicyItem = await req.ReadFromJsonAsync<CertificatePolicyItem>();
        if (string.IsNullOrEmpty(certificatePolicyItem.CertificateName))
        {
            certificatePolicyItem.CertificateName = certificatePolicyItem.DnsNames[0].Replace("*", "wildcard").Replace(".", "-");
        }

        // Function input comes from the request content.
        return req.CreateResponse(HttpStatusCode.OK);
    }
}
