using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Azure.WebJobs.Extensions.HttpApi;

using DurableTask.TypedProxy;

using Kerbee.Models;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Protocols;

using Newtonsoft.Json;
using System.Linq;

namespace Kerbee.Functions;

public class GetServicePrincipals : HttpFunctionBase
{
    public GetServicePrincipals(IHttpContextAccessor httpContextAccessor)
        : base(httpContextAccessor)
    {
    }

    [FunctionName($"{nameof(GetServicePrincipals)}_{nameof(HttpStart)}")]
    public async Task<IActionResult> HttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/serviceprincipals")] HttpRequest req,
        ILogger log)
    {
        if (!User.Identity.IsAuthenticated)
        {
            return Unauthorized();
        }

        string tenantId = Environment.GetEnvironmentVariable("WEBSITE_AUTH_OPENID_ISSUER").Split("/", StringSplitOptions.RemoveEmptyEntries).Last();
        string clientId = Environment.GetEnvironmentVariable("WEBSITE_AUTH_CLIENT_ID");
        string clientSecret = Environment.GetEnvironmentVariable("WEBSITE_AUTH_CLIENT_SECRET");
        string[] downstreamApiScopes = { "https://graph.microsoft.com/.default" };

        try
        {
            if (string.IsNullOrEmpty(tenantId) ||
            string.IsNullOrEmpty(clientId) ||
            string.IsNullOrEmpty(clientSecret))
            {
                throw new Exception("Configuration values are missing.");
            }

            string authority = $"https://login.microsoftonline.com/{tenantId}";
            string issuer = Environment.GetEnvironmentVariable("WEBSITE_AUTH_OPENID_ISSUER");
            string audience = $"api://{clientId}";

            var app = ConfidentialClientApplicationBuilder.Create(clientId)
               .WithAuthority(authority)
               .WithClientSecret(clientSecret)
               .Build();

            var headers = req.Headers;
            var token = string.Empty;
            if (headers.TryGetValue("Authorization", out var authHeader))
            {
                if (authHeader[0].StartsWith("Bearer "))
                {
                    token = authHeader[0].Substring(7, authHeader[0].Length - 7);
                }
                else
                {
                    return new UnauthorizedResult();
                }
            }

            UserAssertion userAssertion = new UserAssertion(token);
            AuthenticationResult result = await app.AcquireTokenOnBehalfOf(downstreamApiScopes, userAssertion).ExecuteAsync();

            string accessToken = result.AccessToken;
            if (accessToken == null)
            {
                throw new Exception("Access Token could not be acquired.");
            }

            var myObj = new { access_token = accessToken };
            var jsonToReturn = JsonConvert.SerializeObject(myObj);
            return new OkObjectResult(jsonToReturn);
        }
        catch (Exception ex)
        {
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
