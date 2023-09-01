using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

using Azure.Core;

using Kerbee.Internal;
using Kerbee.Options;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Applications.Item.RemovePassword;
using Microsoft.Graph.Models;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Kerbee.Graph;

public class GraphService : IGraphService
{
    private readonly ILogger _logger;
    private readonly ManagedIdentityProvider _managedIdentityProvider;
    private readonly IClaimsPrincipalAccessor _claimsPrincipalAccessor;

    public GraphService(
        ManagedIdentityProvider managedIdentityProvider,
        IClaimsPrincipalAccessor claimsPrincipalAccessor,
        IOptions<AzureAdOptions> azureAdOptions,
        IOptions<ManagedIdentityOptions> managedIdentityOptions,
        ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<GraphService>();
        _managedIdentityProvider = managedIdentityProvider;
        _claimsPrincipalAccessor = claimsPrincipalAccessor;
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
            .Where(app => managedApplications.None(x => x.AppId == app.AppId))
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

        var managedIdentity = await _managedIdentityProvider.GetAsync();

        var owners = await client.Applications[applicationObjectId]
            .Owners
            .GetAsync();

        // check if applicationObjectId is already an owner
        if (owners?.Value is not null && owners.Value.Any(x => x.Id == managedIdentity.Id))
        {
            return;
        }

        await client.Applications[applicationObjectId]
            .Owners
            .Ref
            .PostAsync(new()
            {
                OdataId = $"https://graph.microsoft.com/v1.0/directoryObjects/{managedIdentity.Id}"
            });
    }

    public async Task RemoveManagedIdentityAsOwnerOfApplicationAsync(string applicationObjectId)
    {
        var client = GetClientForUser();

        var managedIdentity = await _managedIdentityProvider.GetAsync();

        var owners = await client.Applications[applicationObjectId]
            .Owners
            .GetAsync();

        // check if applicationObjectId is an owner
        if (owners?.Value is not null && owners.Value.None(x => x.Id == managedIdentity.Id))
        {
            return;
        }

        await client.Applications[applicationObjectId]
            .Owners[managedIdentity.Id]
            .Ref
            .DeleteAsync();
    }

    public async Task RemoveCertificateAsync(string applicationObjectId, Guid keyId)
    {
        var client = GetClientForUser();

        var application = await client.Applications[applicationObjectId].GetAsync();
        var key = application?.KeyCredentials?.FirstOrDefault(x => x.KeyId == keyId);

        if (application is not null && key is not null)
        {
            application.KeyCredentials?.Remove(key);
            await client.Applications[applicationObjectId.ToString()].PatchAsync(application);
        }
    }

    public async Task RemoveSecretAsync(string applicationObjectId, Guid keyId)
    {
        var client = GetClientForUser();
        await client.Applications[applicationObjectId]
            .RemovePassword
            .PostAsync(new RemovePasswordPostRequestBody()
        {
            KeyId = keyId
        });
    }

    public async Task<Guid> AddCertificateAsync(string applicationObjectId, byte[] cer)
    {
        // Generate a new certificate for the application
        _ = await _managedIdentityProvider.GetClient()
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
        var application = await _managedIdentityProvider.GetClient()
            .Applications[applicationObjectId]
            .GetAsync();

        var keyId = (application?.KeyCredentials?.FirstOrDefault()?.KeyId)
            ?? throw new Exception($"Failed to add certificate to application {applicationObjectId}");

        _logger.LogInformation("Generated new certificate for application {applicationId}", applicationObjectId);

        return keyId;
    }

    public async Task<PasswordCredential> GenerateSecretAsync(string applicationObjectId, int validityInMonths)
    {
        // Generate a new password for the application
        var password = await _managedIdentityProvider.GetClient()
            .Applications[applicationObjectId]
            .AddPassword
            .PostAsync(new()
            {
                PasswordCredential = new()
                {
                    DisplayName = $"Managed by Kerbee",
                    EndDateTime = DateTimeOffset.UtcNow.AddMonths(validityInMonths),
                    StartDateTime = DateTimeOffset.UtcNow,
                }
            });

        if (password?.SecretText is null)
        {
            throw new Exception($"Failed to add password to application {applicationObjectId}");
        }

        _logger.LogInformation("Generated new password for application {applicationId}", applicationObjectId);

        return password;
    }

    public async Task<Application?> GetApplicationAsync(string applicationObjectId)
    {
        var client = GetClientForUser();
        return await client.Applications[applicationObjectId].GetAsync();
    }

    private async Task<IEnumerable<Application>> GetApplicationsInternalAsync()
    {
        // Get the managed identity by app id
        var managedIdentity = await _managedIdentityProvider.GetAsync();

        _logger.LogInformation("Found managed identity {displayName} with id {objectId}", managedIdentity.DisplayName, managedIdentity.Id);

        // Get the owned objects of the managed identity
        var response = await _managedIdentityProvider.GetClient()
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

        return new GraphServiceClient(DelegatedTokenCredential.Create(
            (_, _) =>
            {
                return new AccessToken(_claimsPrincipalAccessor.AccessToken, DateTime.UtcNow.AddDays(1));
            }));
    }
}
