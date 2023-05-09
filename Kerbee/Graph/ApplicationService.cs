using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;

using Kerbee.Models;
using Kerbee.Options;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;

using OneOf;
using OneOf.Types;

namespace Kerbee.Graph;

internal class ApplicationService : IApplicationService
{
    private readonly GraphClientService _graphClientService;
    private readonly ManagedIdentityOptions _managedIdentityOptions;
    private readonly ILogger<ApplicationService> _logger;

    public ApplicationService(GraphClientService graphClientService, IOptionsSnapshot<ManagedIdentityOptions> managedIdentityOptions, ILoggerFactory loggerFactory)
    {
        _graphClientService = graphClientService;
        _managedIdentityOptions = managedIdentityOptions.Value;
        _logger = loggerFactory.CreateLogger<ApplicationService>();
    }

    public async Task<OneOf<IEnumerable<Application>, UnauthorizedResult, Error<Exception>>> GetUnmanagedApplicationsAsync()
    {
        try
        {
            // Get all applications the user has access to
            var client = _graphClientService.GetClientForUser();
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
                .ToModel()
                .Except(managedApplications)
                .OrderBy(x=>x.DisplayName)
                .ToArray();
        }
        catch (AuthenticationException)
        {
            return new UnauthorizedResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexception error occurred");
            return new Error<Exception>(ex);
        }
    }

    public async Task<OneOf<IEnumerable<Application>, UnauthorizedResult, Error<Exception>>> GetApplicationsAsync()
    {
        try
        {
            var applications = await GetApplicationsInternalAsync();
            return applications.ToArray();
        }
        catch (AuthenticationException)
        {
            return new UnauthorizedResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexception error occurred");
            return new Error<Exception>(ex);
        }
    }

    private async Task<IEnumerable<Application>> GetApplicationsInternalAsync()
    {
        var client = _graphClientService.GetClientForManagedIdentity();

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
                x.QueryParameters.Select = new string[] { "id", "displayName" };
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
            .ToModel()
            .OrderBy(x => x.DisplayName);
    }

    private async Task<Microsoft.Graph.Models.ServicePrincipal> GetManagedIdentity(GraphServiceClient client)
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

    public async Task AddApplicationAsync(Application application)
    {
        var client = _graphClientService.GetClientForUser();

        var managedIdentity = await GetManagedIdentity(client);

        await client.Applications[application.Id.ToString()]
            .Owners
            .Ref
            .PostAsync(new()
            {
                OdataId = $"https://graph.microsoft.com/v1.0/directoryObjects/{managedIdentity.Id}"
            });
    }
}
