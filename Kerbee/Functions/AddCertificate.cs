using System.Threading.Tasks;

using Azure.WebJobs.Extensions.HttpApi;

using Kerbee.Internal;
using Kerbee.Models;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Kerbee.Functions;

public class AddCertificate : HttpFunctionBase
{
    public AddCertificate(IHttpContextAccessor httpContextAccessor)
        : base(httpContextAccessor)
    {
    }

    [FunctionName($"{nameof(AddCertificate)}_{nameof(HttpStart)}")]
    public async Task<IActionResult> HttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api/certificate")] CertificatePolicyItem certificatePolicyItem,
        [DurableClient] IDurableClient starter,
        ILogger log)
    {
        if (!User.Identity.IsAuthenticated)
        {
            return Unauthorized();
        }

        if (!User.HasIssueCertificateRole())
        {
            return Forbid();
        }

        if (!TryValidateModel(certificatePolicyItem))
        {
            return ValidationProblem(ModelState);
        }

        if (string.IsNullOrEmpty(certificatePolicyItem.CertificateName))
        {
            certificatePolicyItem.CertificateName = certificatePolicyItem.DnsNames[0].Replace("*", "wildcard").Replace(".", "-");
        }

        // Function input comes from the request content.
        return Ok();
    }
}
