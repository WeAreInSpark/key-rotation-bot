using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Azure.Data.Tables;

using Kerbee.Entities;
using Kerbee.Internal;
using Kerbee.Models;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;

namespace Kerbee.Graph;

internal class ApplicationService : IApplicationService
{
    private readonly ILogger<ApplicationService> _logger;
    private readonly TableClient _tableClient;
    private readonly IGraphService _graphService;

    public ApplicationService(IConfiguration configuration, ILoggerFactory loggerFactory, IGraphService graphService)
    {
        _logger = loggerFactory.CreateLogger<ApplicationService>();
        _tableClient = new TableClient(configuration["AzureWebJobsStorage"], "applications");
        _graphService = graphService;
    }

    public async Task AddApplicationAsync(Application application)
    {
        // Create the table if it doesn't exist
        await _tableClient.CreateIfNotExistsAsync();

        await _tableClient.AddEntityAsync(application.ToEntity());
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
            .Where(application => graphApplications.None(x => x.Id == application.Id.ToString()));

        foreach (var application in applicationsPendingRemoval)
        {
            await DeleteApplicationAsync(application);
            applications.Remove(application);
        }

        // Add applications that are owned by kerbee in the graph but not in the table
        var applicationsPendingManagement = graphApplications
            .Where(x => applications.None(application => application.Id.ToString() == x.Id));

        foreach (var graphApplication in applicationsPendingManagement)
        {
            var application = graphApplication.ToModel();
            await AddApplicationAsync(application);
            applications.Add(application);
        }

        return applications;
    }
}
