using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Azure;
using Azure.Data.Tables;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;

using Kerbee.Entities;
using Kerbee.Internal;
using Kerbee.Models;
using Kerbee.Options;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kerbee.Graph;

internal class ApplicationService : IApplicationService
{
    private readonly ILogger<ApplicationService> _logger;
    private readonly TableClient _tableClient;
    private readonly IGraphService _graphService;
    private readonly CertificateClient _certificateClient;
    private readonly SecretClient _secretClient;
    private readonly IOptions<KerbeeOptions> _kerbeeOptions;

    public ApplicationService(
        IConfiguration configuration,
        ILoggerFactory loggerFactory,
        IGraphService graphService,
        CertificateClient certificateClient,
        SecretClient secretClient,
        IOptions<KerbeeOptions> kerbeeOptions)
    {
        _logger = loggerFactory.CreateLogger<ApplicationService>();
        _tableClient = new TableClient(configuration["AzureWebJobsStorage"], "applications");
        _graphService = graphService;
        _certificateClient = certificateClient;
        _secretClient = secretClient;
        _kerbeeOptions = kerbeeOptions;
    }

    public async Task AddApplicationAsync(Application application, bool addOwner)
    {
        // Create the table if it doesn't exist
        await _tableClient.CreateIfNotExistsAsync();

        if (application.CreatedOn == default)
        {
            application.CreatedOn = DateTime.UtcNow;
        }

        await _tableClient.AddEntityAsync(application.ToEntity());

        if (addOwner)
        {
            await _graphService.MakeManagedIdentityOwnerOfApplicationAsync(application.Id.ToString());
        }
    }

    public async Task DeleteApplicationAsync(Application application)
    {
        // Create the table if it doesn't exist
        await _tableClient.CreateIfNotExistsAsync();

        await _tableClient.DeleteEntityAsync("kerbee", application.Id.ToString());
    }

    public async Task<IEnumerable<Application>> GetApplicationsAsync(DateTime? expiryDate = null)
    {
        // Create the table if it doesn't exist
        await _tableClient.CreateIfNotExistsAsync();

        var applicationEntities = expiryDate.HasValue
            ? _tableClient.QueryAsync<ApplicationEntity>(x => x.PartitionKey == "kerbee" && x.ExpiresOn <= expiryDate)
            : _tableClient.QueryAsync<ApplicationEntity>(x => x.PartitionKey == "kerbee");

        var applications = new List<Application>();
        await foreach (var applicationEntity in applicationEntities)
        {
            applications.Add(applicationEntity.ToModel());
        }

        return applications;
    }

    public async Task UnmanageApplicationAsync(string applicationId)
    {
        var application = (await GetApplicationsAsync()).FirstOrDefault(x => x.Id.ToString() == applicationId);
        if (application == null)
        {
            return;
        }

        await _graphService.RemoveManagedIdentityAsOwnerOfApplicationAsync(applicationId);
        await DeleteApplicationAsync(application);
    }

    public async Task UpdateApplications()
    {
        var applications = (await GetApplicationsAsync()).ToList();
        var graphApplications = await _graphService.GetApplicationsAsync();

        // Remove applications that are no longer owned by kerbee in the graph
        var applicationsPendingRemoval = applications
            .Where(application => graphApplications.None(x => x.Id == application.Id.ToString()))
            .ToArray();

        foreach (var application in applicationsPendingRemoval)
        {
            await DeleteApplicationAsync(application);
            applications.Remove(application);
        }

        // Add applications that are owned by kerbee in the graph but not in the table
        var applicationsPendingManagement = graphApplications
            .Where(x => applications.None(application => application.Id.ToString() == x.Id))
            .ToArray();

        foreach (var graphApplication in applicationsPendingManagement)
        {
            var application = graphApplication.ToModel();
            await AddApplicationAsync(application, false);
            applications.Add(application);
        }
    }

    public async Task RenewCertificate(Application application)
    {
        _logger.LogInformation("Renewing certificate for application {applicationId}", application.Id);

        // Generate certificate in Azure Key Vault
        var policy = CertificatePolicy.Default;
        policy.ValidityInMonths = _kerbeeOptions.Value.ValidityInMonths;

        var certificateOperation = await _certificateClient.StartCreateCertificateAsync(
            application.KeyName, policy);

        var certificate = await certificateOperation.WaitForCompletionAsync();

        if (certificate.Value.Properties.CreatedOn is null ||
            certificate.Value.Properties.ExpiresOn is null)
        {
            throw new Exception("Certificate creation failed");
        }

        // Add the certificate to the application in the graph
        var keyId = await _graphService.AddCertificateAsync(application.Id.ToString(), certificate.Value.Cer);

        // Update the application in the table
        var applicationEntity = application.ToEntity();
        applicationEntity.KeyType = KeyType.Certificate;
        applicationEntity.KeyVaultKeyId = certificate.Value.KeyId.ToString();
        applicationEntity.KeyId = keyId;
        applicationEntity.CreatedOn = certificate.Value.Properties.CreatedOn.Value;
        applicationEntity.ExpiresOn = certificate.Value.Properties.ExpiresOn.Value;

        await _tableClient.UpdateEntityAsync(applicationEntity, ETag.All);
    }

    public async Task RenewSecret(Application application)
    {
        _logger.LogInformation("Renewing secret for application {applicationId}", application.Id);

        // Add the secret to the application in the graph
        var azureADSecret = await _graphService.GenerateSecretAsync(application.Id.ToString(), _kerbeeOptions.Value.ValidityInMonths);

        if (azureADSecret.StartDateTime is null || azureADSecret.EndDateTime is null)
        {
            throw new Exception("Secret creation failed");
        }

        var keyVaultSecret = new KeyVaultSecret(application.KeyName, azureADSecret.SecretText)
        {
            Properties =
            {
                ExpiresOn = azureADSecret.EndDateTime,
                NotBefore = azureADSecret.StartDateTime
            }
        };

        var response = await _secretClient.SetSecretAsync(keyVaultSecret);

        if (response is null)
        {
            throw new Exception("Secret creation failed");
        }

        // Update the application in the table
        var applicationEntity = application.ToEntity();
        applicationEntity.KeyType = KeyType.Secret;
        applicationEntity.KeyVaultKeyId = response.Value.Id.ToString();
        applicationEntity.KeyId = azureADSecret.KeyId;
        applicationEntity.CreatedOn = azureADSecret.StartDateTime.Value;
        applicationEntity.ExpiresOn = azureADSecret.EndDateTime.Value;

        await _tableClient.UpdateEntityAsync(applicationEntity, ETag.All);
    }

    public async Task RenewKeyAsync(string applicationId)
    {
        var application = (await GetApplicationsAsync()).FirstOrDefault(x => x.Id.ToString() == applicationId);
        if (application == null)
        {
            return;
        }

        await RenewKeyAsync(application);
    }

    public async Task RenewKeyAsync(Application application)
    {
        var task = application.KeyType switch
        {
            KeyType.Certificate => RenewCertificate(application),
            KeyType.Secret => RenewSecret(application),
            _ => Task.CompletedTask
        };

        await task;
    }

    public async Task RemoveKeyAsync(string applicationId)
    {
        var application = (await GetApplicationsAsync()).FirstOrDefault(x => x.Id.ToString() == applicationId);
        if (application == null)
        {
            return;
        }

        await RemoveKeyAsync(application);
    }

    public async Task RemoveKeyAsync(Application application)
    {
        if (application.KeyId is null)
        {
            _logger.LogWarning("Application {applicationId} does not have a key", application.Id);
            return;
        }

        if (application.KeyType == KeyType.Certificate)
        {
            await _graphService.RemoveCertificateAsync(application.Id.ToString(), new Guid(application.KeyId));
            await _certificateClient.StartDeleteCertificateAsync(application.KeyName);
        }
        else if (application.KeyType == KeyType.Secret)
        {
            await _graphService.RemoveSecretAsync(application.Id.ToString(), new Guid(application.KeyId));
            await _secretClient.StartDeleteSecretAsync(application.KeyName);
        }
    }

    public async Task PurgeKeys(Application application)
    {
        var graphApplication = await _graphService.GetApplicationAsync(application.Id.ToString());

        if (graphApplication is null)
        {
            return;
        }

        // Remove expired secrets
        var expiredSecrets = graphApplication.PasswordCredentials?
            .Where(x => x.EndDateTime <= DateTime.UtcNow)
            .Where(x => x.KeyId.HasValue)
            .ToArray() ?? Array.Empty<Microsoft.Graph.Models.PasswordCredential>();

        await Task.WhenAll(expiredSecrets.Select(x => _graphService.RemoveSecretAsync(application.Id.ToString(), x.KeyId!.Value)));

        // Remove expired certificates
        var expiredCertificates = graphApplication.KeyCredentials?
            .Where(x => x.EndDateTime <= DateTime.UtcNow)
            .Where(x => x.KeyId.HasValue)
            .ToArray() ?? Array.Empty<Microsoft.Graph.Models.KeyCredential>();

        await Task.WhenAll(expiredCertificates.Select(x => _graphService.RemoveCertificateAsync(application.Id.ToString(), x.KeyId!.Value)));
    }
}
