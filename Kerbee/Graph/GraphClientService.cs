using System;
using System.Linq;
using System.Security.Authentication;

using Azure.Core;
using Azure.Identity;

using Kerbee.Options;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;

namespace Kerbee.Graph;

internal class GraphClientService
{
    private readonly AzureAdOptions _aadOptions;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger _logger;

    public GraphClientService(IOptionsSnapshot<AzureAdOptions> aadOptions, IHttpContextAccessor httpContextAccessor, ILoggerFactory loggerFactory)
    {
        _aadOptions = aadOptions.Value;
        _httpContextAccessor = httpContextAccessor;
        _logger = loggerFactory.CreateLogger<GraphClientService>();
    }

    public GraphServiceClient GetClientForUser()
    {
        var context = _httpContextAccessor.HttpContext;

        if (!context.User.Identity.IsAuthenticated)
        {
            throw new AuthenticationException();
        }

        if (!context.Request.Headers.TryGetValue("x-ms-token-aad-access-token", out var accessTokenValues))
        {
            throw new AuthenticationException();
        }

        var accessToken = accessTokenValues.First();

        _logger.LogInformation("Access token: {accessToken}", accessToken);

        _logger.LogInformation("Issuer: {issuer}, tenantId: {tenantId}, clientId: {clientId}, clientSecret: {clientSecret}",
            _aadOptions.Issuer,
            _aadOptions.TenantId,
            _aadOptions.ClientId,
            _aadOptions.ClientSecret);

        return new GraphServiceClient(DelegatedTokenCredential.Create(
            (_, _) =>
            {
                return new AccessToken(accessToken, DateTime.UtcNow.AddDays(1));
            }));
    }

    public GraphServiceClient GetClientForManagedIdentity()
    {
        return new GraphServiceClient(new DefaultAzureCredential());
    }
}
