using System.Threading.Tasks;

using Azure.WebJobs.Extensions.HttpApi;

using DurableTask.TypedProxy;

using Kerbee.Internal;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Kerbee.Functions;

public class RenewCertificate : HttpFunctionBase
{
    public RenewCertificate(IHttpContextAccessor httpContextAccessor)
        : base(httpContextAccessor)
    {
    }

    [FunctionName($"{nameof(RenewCertificate)}_{nameof(HttpStart)}")]
    public async Task<IActionResult> HttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api/certificate/{certificateName}/renew")] HttpRequest req,
        string certificateName,
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


        return Ok();
    }
}
