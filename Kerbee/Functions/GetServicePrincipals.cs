using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;

using Azure.Core;
using Azure.WebJobs.Extensions.HttpApi;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;

namespace Kerbee.Functions;

public class GetServicePrincipals : HttpFunctionBase
{
    private ILogger<GetServicePrincipals> _logger;

    public GetServicePrincipals(IHttpContextAccessor httpContextAccessor, ILoggerFactory loggerFactory)
        : base(httpContextAccessor)
    {
        _logger = loggerFactory.CreateLogger<GetServicePrincipals>();
    }

    [FunctionName($"{nameof(GetServicePrincipals)}_{nameof(HttpStart)}")]
    public async Task<IActionResult> HttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/serviceprincipals")] HttpRequest req)
    {
        if (!User.Identity.IsAuthenticated)
        {
            return Unauthorized();
        }

        if (!req.Headers.TryGetValue("x-ms-token-aad-access-token", out var accessTokenValues))
        {
            return Unauthorized();
        }

        try
        {
            var accessToken = accessTokenValues.First();

            _logger.LogInformation("Access token: {accessToken}", accessToken);

            var issuer = new Uri(Environment.GetEnvironmentVariable("WEBSITE_AUTH_OPENID_ISSUER"));
            var tenantId = issuer.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries).First();
            var clientId = Environment.GetEnvironmentVariable("WEBSITE_AUTH_CLIENT_ID");
            var clientSecret = Environment.GetEnvironmentVariable("MICROSOFT_PROVIDER_AUTHENTICATION_SECRET");

            _logger.LogInformation("Issuer: {issuer}, tenantId: {tenantId}, clientId: {clientId}, clientSecret: {clientSecret}", issuer, tenantId, clientId, clientSecret);

            var client = new GraphServiceClient(DelegatedTokenCredential.Create(
                (_, _) =>
                {
                    return new AccessToken(accessToken, DateTime.UtcNow.AddDays(1));
                }));
            var applications = await client.Applications.GetAsync();

            return new OkObjectResult(applications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexception error occurred");
            throw;
        }
    }
}
