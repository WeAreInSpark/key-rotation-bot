using System;
using System.Security.Authentication;

using Azure.Core;
using Azure.Identity;

using Kerbee.Internal;

using Microsoft.Extensions.Logging;
using Microsoft.Graph;

namespace Kerbee.Graph;

internal class GraphClientService
{
    private readonly ILogger _logger;
    private readonly IClaimsPrincipalAccessor _claimsPrincipalAccessor;

    public GraphClientService(IClaimsPrincipalAccessor claimsPrincipalAccessor, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<GraphClientService>();
        _claimsPrincipalAccessor = claimsPrincipalAccessor;
    }

    public GraphServiceClient GetClientForUser()
    {
        if (_claimsPrincipalAccessor.Principal?.Identity?.IsAuthenticated != true)
        {
            throw new AuthenticationException();
        }

        if (_claimsPrincipalAccessor.AccessToken is null)
        {
            throw new AuthenticationException();
        }

        _logger.LogInformation("Access token: {accessToken}", _claimsPrincipalAccessor.AccessToken);

        return new GraphServiceClient(DelegatedTokenCredential.Create(
            (_, _) =>
            {
                return new AccessToken(_claimsPrincipalAccessor.AccessToken, DateTime.UtcNow.AddDays(1));
            }));
    }

    public GraphServiceClient GetClientForManagedIdentity()
    {
        return new GraphServiceClient(new DefaultAzureCredential());
    }
}
