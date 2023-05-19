using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;

using Azure.Core;
using Azure.Identity;

using Kerbee.Internal;
using Kerbee.Options;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace Kerbee.Graph;

public class GraphService : IGraphService
{
    private readonly ILogger _logger;
    private readonly IClaimsPrincipalAccessor _claimsPrincipalAccessor;
    private readonly AzureAdOptions _azureAdOptions;
    private readonly ManagedIdentityOptions _managedIdentityOptions;

    public GraphService(
        IClaimsPrincipalAccessor claimsPrincipalAccessor,
        IOptions<AzureAdOptions> azureAdOptions,
        IOptionsSnapshot<ManagedIdentityOptions> managedIdentityOptions,
        ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<GraphService>();
        _claimsPrincipalAccessor = claimsPrincipalAccessor;
        _azureAdOptions = azureAdOptions.Value;
        _managedIdentityOptions = managedIdentityOptions.Value;
    }

    public async Task<IEnumerable<Application>> GetUnmanagedApplicationsAsync()
    {
        // Get all applications the user has access to
        var client = GetClientForUser();
        var response = await client
            .Applications
            .GetAsync(x =>
            {
                x.QueryParameters.Select = new string[] { "id", "displayName" };
                x.QueryParameters.Top = 999;
            });

        if (response?.Value is null)
        {
            return Array.Empty<Application>();
        }

        // Get all managed applications
        var managedApplications = await GetApplicationsInternalAsync();

        // Return all applications except the managed ones
        return response.Value
            .Except(managedApplications)
            .OrderBy(x => x.DisplayName)
            .ToArray();
    }

    public async Task<IEnumerable<Application>> GetApplicationsAsync()
    {
        var applications = await GetApplicationsInternalAsync();
        return applications.ToArray();
    }

    public async Task MakeManagedIdentityOwnerOfApplicationAsync(Application application)
    {
        var client = GetClientForUser();

        var managedIdentity = await GetManagedIdentity(client);

        if (application.Id is null)
        {
            throw new ArgumentException("Application id is null");
        }

        await client.Applications[application.Id.ToString()]
            .Owners
            .Ref
            .PostAsync(new()
            {
                OdataId = $"https://graph.microsoft.com/v1.0/directoryObjects/{managedIdentity.Id}"
            });
    }

    private async Task<IEnumerable<Application>> GetApplicationsInternalAsync()
    {
        var client = GetClientForManagedIdentity();

        // Get the managed identity by app id
        var managedIdentity = await GetManagedIdentity(client);

        _logger.LogInformation("Found managed identity {displayName} with id {objectId}", managedIdentity.DisplayName, managedIdentity.Id);

        // Get the owned objects of the managed identity
        var response = await client
            .ServicePrincipals[managedIdentity.Id]
            .OwnedObjects
            .GraphApplication
            .GetAsync(x =>
            {
                x.QueryParameters.Select = new string[] { "id", "displayName", "appId" };
                x.QueryParameters.Top = 999;
            });

        // Make sure the owned object is not null
        if (response?.Value?.FirstOrDefault() is null)
        {
            _logger.LogInformation("No owned applications found for managed identity {displayName}", managedIdentity.DisplayName);
            return Array.Empty<Application>();
        }

        var application = response.Value.First();

        _logger.LogInformation("Found owned application {displayName} with id {objectId}", application.DisplayName, application.Id);

        // Generate a new key for the application
        var password = await client
            .Applications[application.Id]
            .AddPassword
            .PostAsync(new()
            {
                PasswordCredential = new()
                {
                    DisplayName = $"Foo {DateTime.UtcNow}",
                    EndDateTime = DateTimeOffset.UtcNow.AddDays(90),
                    StartDateTime = DateTimeOffset.UtcNow,
                }
            });

        _logger.LogInformation("Generated new password for application {displayName}: {password}", application.DisplayName, password.SecretText);

        return response.Value
            .OrderBy(x => x.DisplayName);
    }

    private async Task<ServicePrincipal> GetManagedIdentity(GraphServiceClient client)
    {
        var managedIdentities = await client
            .ServicePrincipals
            .GetAsync(x =>
            {
                x.QueryParameters.Filter = $"appId eq '{_managedIdentityOptions.ClientId}'";
                x.QueryParameters.Select = new string[] { "id", "displayName" };
            });

        return managedIdentities?.Value?.FirstOrDefault()
            ?? throw new Exception("Managed identity not found");
    }

    private GraphServiceClient GetClientForUser()
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

    private GraphServiceClient GetClientForManagedIdentity()
    {
        return new GraphServiceClient(new DefaultAzureCredential(
            new DefaultAzureCredentialOptions
            {
                TenantId = _azureAdOptions.TenantId,
            }));
    }
}
