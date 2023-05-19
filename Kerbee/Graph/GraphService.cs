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
                x.QueryParameters.Select = new string[] { "id", "displayName", "appId" };
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

    public async Task MakeManagedIdentityOwnerOfApplicationAsync(string applicationObjectId)
    {
        var client = GetClientForUser();

        var managedIdentity = await GetManagedIdentity(client);

        await client.Applications[applicationObjectId]
            .Owners
            .Ref
            .PostAsync(new()
            {
                OdataId = $"https://graph.microsoft.com/v1.0/directoryObjects/{managedIdentity.Id}"
            });
    }

    public async Task<Guid> AddCertificateAsync(string applicationObjectId, byte[] cer)
    {
        var client = GetClientForManagedIdentity();

        // Generate a new certificate for the application
        _ = await client
            .Applications[applicationObjectId]
            .PatchAsync(new()
            {
                KeyCredentials = new()
                {
                    new KeyCredential
                    {
                        DisplayName = "Managed by Kerbee",
                        Key = cer,
                        Type = "AsymmetricX509Cert",
                        Usage = "Verify",
                    }
                }
            });

        // Get the updated application in order to get the key id
        var application = await client
            .Applications[applicationObjectId]
            .GetAsync();

        var keyId = application?.KeyCredentials?.FirstOrDefault()?.KeyId;
        return keyId is null
            ? throw new Exception($"Failed to add certificate to application {applicationObjectId}")
            : keyId.Value;
    }

    public async Task<string> GeneratePasswordAsync(Application application)
    {
        var client = GetClientForManagedIdentity();

        // Generate a new password for the application
        var password = await client
            .Applications[application.Id]
            .AddPassword
            .PostAsync(new()
            {
                PasswordCredential = new()
                {
                    DisplayName = $"Managed by Kerbee",
                    EndDateTime = DateTimeOffset.UtcNow.AddDays(90),
                    StartDateTime = DateTimeOffset.UtcNow,
                }
            });

        if (password?.SecretText is null)
        {
            throw new Exception($"Failed to add password to application {application.DisplayName}");
        }

        _logger.LogInformation("Generated new password for application {displayName}", application.DisplayName);

        return password.SecretText;
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
