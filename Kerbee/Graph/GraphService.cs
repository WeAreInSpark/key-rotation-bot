﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;

using Azure.Core;

using Kerbee.Internal;
using Kerbee.Options;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Applications.Item.AddPassword;
using Microsoft.Graph.Applications.Item.RemovePassword;
using Microsoft.Graph.Models;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Http.HttpClientLibrary.Middleware.Options;

namespace Kerbee.Graph;

public class GraphService : IGraphService
{
    private readonly ILogger _logger;
    private readonly ManagedIdentityProvider _managedIdentityProvider;
    private readonly IClaimsPrincipalAccessor _claimsPrincipalAccessor;
    private readonly IOptions<WebsiteOptions> _websiteOptions;

    public GraphService(
        ManagedIdentityProvider managedIdentityProvider,
        IClaimsPrincipalAccessor claimsPrincipalAccessor,
        IOptions<WebsiteOptions> websiteOptions,
        ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<GraphService>();
        _managedIdentityProvider = managedIdentityProvider;
        _claimsPrincipalAccessor = claimsPrincipalAccessor;
        _websiteOptions = websiteOptions;
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

        if (managedIdentity is null)
        {
            return;
        }

        if (await IsApplicationOwnerAsync(applicationObjectId, managedIdentity.Id))
        {
            return;
        }

        _logger.LogInformation("Make managed identity {directoryObjectId} owner of application {applicationObjectId}", managedIdentity.Id, applicationObjectId);

        // Make the applicationObjectId an owner of the application
        await client.Applications[applicationObjectId]
            .Owners
            .Ref
            .PostAsync(new()
            {
                OdataId = $"https://graph.microsoft.com/v1.0/directoryObjects/{managedIdentity.Id}"
            });

        _logger.LogInformation("Waiting for managed identity {directoryObjectId} to be owner of application {applicationObjectId}", managedIdentity.Id, applicationObjectId);

        // Wait for the application to be an owner
        var retryCount = 0;
        while (true)
        {
            if (await IsApplicationOwnerAsync(applicationObjectId, managedIdentity.Id))
            {
                break;
            }

            if (retryCount > 20)
            {
                throw new Exception($"Failed to make managed identity {managedIdentity.DisplayName} owner of application {applicationObjectId}");
            }

            retryCount++;
            await Task.Delay(2000);
        }

        _logger.LogInformation("Managed identity {directoryObjectId} is owner of application {applicationObjectId}", managedIdentity.Id, applicationObjectId);
    }

    public async Task RemoveManagedIdentityAsOwnerOfApplicationAsync(string applicationObjectId)
    {
        var client = GetClientForUser();

        var managedIdentity = await _managedIdentityProvider.GetAsync();

        if (managedIdentity is null)
        {
            return;
        }

        if (!await IsApplicationOwnerAsync(applicationObjectId, managedIdentity.Id))
        {
            return;
        }

        await client.Applications[applicationObjectId]
            .Owners[managedIdentity.Id]
            .Ref
            .DeleteAsync();

        // Wait for the application to be an owner
        var retryCount = 0;
        while (true)
        {
            if (!await IsApplicationOwnerAsync(applicationObjectId, managedIdentity.Id))
            {
                break;
            }

            if (retryCount > 20)
            {
                throw new Exception($"Failed to remove managed identity {managedIdentity.DisplayName} as owner of application {applicationObjectId}");
            }

            retryCount++;
            await Task.Delay(2000);
        }
    }

    public async Task RemoveCertificateAsync(string applicationObjectId, string keyId)
    {
        var client = GetClientForUser();

        var application = await client.Applications[applicationObjectId].GetAsync();
        var key = application?.KeyCredentials?
            .Where(x => x.CustomKeyIdentifier is not null)
            .FirstOrDefault(x => Convert.ToBase64String(x.CustomKeyIdentifier!) == keyId);

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

    public async Task AddCertificateAsync(string applicationObjectId, byte[] cer, params string[] keysToReplace)
    {
        // Get the existing application in order to add a new certificate without removing the existing ones
        // Explicitly select keyCredentials in order to get the public key details
        var application = await _managedIdentityProvider.GetClient()
            .Applications[applicationObjectId]
            .GetAsync(x => x.QueryParameters.Select = new string[] { "id", "appId", "keyCredentials" })
            ?? throw new Exception($"Failed to get application {applicationObjectId}");

        var keyCredentials = (application.KeyCredentials ?? new())
            .Where(x => x.CustomKeyIdentifier is null || keysToReplace.Contains(Convert.ToBase64String(x.CustomKeyIdentifier)) == false)
            .Select(x =>
                new KeyCredential()
                {
                    DisplayName = x.DisplayName,
                    Key = x.Key,
                    Type = x.Type,
                    Usage = x.Usage,
                })
            .Union(
                new List<KeyCredential>()
                {
                    new()
                    {
                        DisplayName = $"Managed by Kerbee ({_websiteOptions.Value.SiteName})",
                        Key = cer,
                        Type = "AsymmetricX509Cert",
                        Usage = "Verify",
                    }
                }
            )
            .ToList();

        // Generate a new certificate for the application
        _ = await _managedIdentityProvider.GetClient()
            .Applications[applicationObjectId]
            .PatchAsync(new()
            {
                KeyCredentials = keyCredentials
            });

        // Get the updated application in order to get the key id
        application = await GetApplicationAsync(applicationObjectId);

        _logger.LogInformation("Generated new certificate for application {applicationId}", applicationObjectId);
    }

    public async Task<PasswordCredential> GenerateSecretAsync(string applicationObjectId, int validityInMonths)
    {
        // Generate a new password for the application
        var request = _managedIdentityProvider.GetClient().Applications[applicationObjectId].AddPassword;
        var body = new AddPasswordPostRequestBody()
        {
            PasswordCredential = new()
            {
                DisplayName = $"Managed by Kerbee ({_websiteOptions.Value.SiteName})",
                EndDateTime = DateTimeOffset.UtcNow.AddMonths(validityInMonths),
                StartDateTime = DateTimeOffset.UtcNow,
            }
        };

        const int maxRetries = 5;

        var requestOptions = new List<IRequestOption>
        {
            new RetryHandlerOption()
            {
                MaxRetry = maxRetries,
                Delay = 2,
                ShouldRetry = (delay, attempt, httpResponse) =>
                {
                    if (httpResponse?.StatusCode == System.Net.HttpStatusCode.OK) { return false; }
                    if (httpResponse?.StatusCode == System.Net.HttpStatusCode.Unauthorized) { return false; }
                    if (attempt > maxRetries) { return false; }
                    _logger.LogInformation("Retrying request ({attempt}) to generate secret for application {applicationId}", attempt, applicationObjectId);
                    return true;
                }
            }
        };

        var password = await request.PostAsync(body, requestConfiguration => requestConfiguration.Options = requestOptions);

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

    private async Task<bool> IsApplicationOwnerAsync(string applicationObjectId, string? directoryObjectId)
    {
        var client = GetClientForUser();

        var owners = await client.Applications[applicationObjectId]
            .Owners
            .GetAsync();

        return owners?.Value is not null
            && owners.Value.Any(x => x.Id == directoryObjectId);
    }
}
