﻿using System;
using System.Linq;
using System.Threading.Tasks;

using Azure.Identity;

using Kerbee.Options;

using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace Kerbee.Graph;

public class ManagedIdentityProvider
{
    private readonly GraphServiceClient _managedIdentityClient;
    private readonly Task<ServicePrincipal> _loadManagedIdentity;
    private readonly IOptions<AzureAdOptions> _azureAdOptions;
    private readonly IOptions<ManagedIdentityOptions> _managedIdentityOptions;

    public ManagedIdentityProvider(
        IOptions<AzureAdOptions> azureAdOptions,
        IOptions<ManagedIdentityOptions> managedIdentityOptions)
    {
        _azureAdOptions = azureAdOptions;
        _managedIdentityOptions = managedIdentityOptions;

        _managedIdentityClient = new GraphServiceClient(new DefaultAzureCredential(
            new DefaultAzureCredentialOptions
            {
                TenantId = _azureAdOptions.Value.TenantId,
            }));

        _loadManagedIdentity = new Func<Task<ServicePrincipal>>(async () =>
        {
            var managedIdentities = await _managedIdentityClient
            .ServicePrincipals
            .GetAsync(x =>
            {
                x.QueryParameters.Filter = $"appId eq '{_managedIdentityOptions.Value.ClientId}'";
                x.QueryParameters.Select = new string[] { "id", "displayName" };
            });

            return managedIdentities?.Value?.FirstOrDefault()
                ?? throw new Exception("Managed identity not found");
        })();
    }

    public GraphServiceClient GetClient()
    {
        return _managedIdentityClient;
    }

    public async Task<ServicePrincipal> GetAsync()
    {
        return await _loadManagedIdentity;
    }
}