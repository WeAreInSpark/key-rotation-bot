using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Azure;
using Azure.Data.Tables;
using Azure.Security.KeyVault.Certificates;

using Kerbee.Entities;
using Kerbee.Internal;
using Kerbee.Models;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Kerbee.Graph;

internal class ApplicationService : IApplicationService
{
    private readonly ILogger<ApplicationService> _logger;
    private readonly TableClient _tableClient;
    private readonly IGraphService _graphService;
    private readonly CertificateClient _certificateClient;

    public ApplicationService(
        IConfiguration configuration,
        ILoggerFactory loggerFactory,
        IGraphService graphService,
        CertificateClient certificateClient)
    {
        _logger = loggerFactory.CreateLogger<ApplicationService>();
        _tableClient = new TableClient(configuration["AzureWebJobsStorage"], "applications");
        _graphService = graphService;
        _certificateClient = certificateClient;
    }

    public async Task AddApplicationAsync(Application application)
    {
        // Create the table if it doesn't exist
        await _tableClient.CreateIfNotExistsAsync();

        await _tableClient.AddEntityAsync(application.ToEntity());

        await _graphService.MakeManagedIdentityOwnerOfApplicationAsync(application.Id.ToString());

        application.KeyType = KeyType.Certificate;
        await RotateKey(application);
    }

    public async Task DeleteApplicationAsync(Application application)
    {
        // Create the table if it doesn't exist
        await _tableClient.CreateIfNotExistsAsync();

        await _tableClient.DeleteEntityAsync("kerbee", application.Id.ToString());
    }

    public async Task<IEnumerable<Application>> GetApplicationsAsync()
    {
        // Create the table if it doesn't exist
        await _tableClient.CreateIfNotExistsAsync();

        var applicationEntitiesQuery = _tableClient
            .QueryAsync<ApplicationEntity>(x => x.PartitionKey == "kerbee");

        var applications = new List<Application>();
        await foreach (var applicationEntity in applicationEntitiesQuery)
        {
            applications.Add(applicationEntity.ToModel());
        }

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
            await AddApplicationAsync(application);
            applications.Add(application);
        }

        return applications;
    }

    public async Task RotateKey(Application application)
    {
        _logger.LogInformation("Rotating key for application {applicationId}", application.Id);

        // Generate certificate in Azure Key Vault
        var policy = CertificatePolicy.Default;
        policy.ValidityInMonths = 3;

        var certificateOperation = await _certificateClient.StartCreateCertificateAsync(
            application.Id.ToString(), policy);

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
        applicationEntity.KeyVaultKeyId = certificate.Value.KeyId.ToString();
        applicationEntity.KeyId = keyId;
        applicationEntity.CreatedOn = certificate.Value.Properties.CreatedOn.Value;
        applicationEntity.ExpiresOn = certificate.Value.Properties.ExpiresOn.Value;

        await _tableClient.UpdateEntityAsync(applicationEntity, ETag.All);
    }
}
