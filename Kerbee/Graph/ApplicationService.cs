using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;

using Kerbee.Options;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph.Drives.Item.Items.Item.Workbook.Functions.Rank_Avg;
using Microsoft.Graph.Models;

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

    public async Task<OneOf<IEnumerable<Application>, UnauthorizedResult, Error<Exception>>> GetApplicationsAsync()
    {
        try
        {
            var client = _graphClientService.GetClientForUser();
            var response = await client
                .Applications
                .GetAsync(x => x.QueryParameters.Select = new string[] { "id", "displayName" });

            return response.Value;
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

    public async Task<OneOf<IEnumerable<Application>, UnauthorizedResult, Error<Exception>>> GetManagedApplicationsAsync()
    {
        try
        {
            var client = _graphClientService.GetClientForManagedIdentity();
            
            // Get the managed identity by app id
            var managedIdentities = await client
                .ServicePrincipals
                .GetAsync(x => {
                    x.QueryParameters.Filter = $"appId eq '{_managedIdentityOptions.ClientId}'";
                    x.QueryParameters.Select = new string[] { "id", "displayName" };
                });

            var managedIdentity = managedIdentities.Value.FirstOrDefault()
                ?? throw new Exception("Managed identity not found");

            _logger.LogInformation("Found managed identity {displayName} with id {objectId}", managedIdentity.DisplayName, managedIdentity.Id);

            // Get the owned objects of the managed identity
            var response = await client
                .ServicePrincipals[managedIdentity.Id]
                .OwnedObjects
                .GraphApplication
                .GetAsync(x =>
                {
                    x.QueryParameters.Select = new string[] { "id", "displayName" };
                });

            var application = response.Value.FirstOrDefault();

            // Make sure the owned object is not null
            if (application is null)
            {
                _logger.LogInformation("No owned applications found for managed identity {displayName}", managedIdentity.DisplayName);
                return response.Value;
            }

            _logger.LogInformation("Found owned application {displayName} with id {objectId}", application.DisplayName, application.Id);

            // Generate a new key for the application
            var password = await client
                .Applications[application.Id]
                .AddPassword
                .PostAsync(new()
                {
                     PasswordCredential = new()
                     {
                         DisplayName = "Foo",
                         EndDateTime = DateTimeOffset.UtcNow.AddDays(90),
                         StartDateTime = DateTimeOffset.UtcNow,
                         //Hint = "Bar",
                         //KeyId = Guid.NewGuid(),
                         //SecretText = "Bar",
                     }
                });

            _logger.LogInformation("Generated new password for application {displayName}: {password}", application.DisplayName, password.SecretText);

            return response.Value;
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
}
