using System;
using System.Text.Json;
using System.Text.Json.Serialization;

using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;

using Kerbee.Graph;
using Kerbee.Internal;
using Kerbee.Options;

using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

[assembly: FunctionsStartup(typeof(Kerbee.Startup))]

namespace Kerbee;

public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        var context = builder.GetContext();

        // Add Options
        builder.Services.AddOptions<KerbeeOptions>()
               .Bind(context.Configuration.GetSection(KerbeeOptions.Kerbee))
               .ValidateDataAnnotations();

        builder.Services.AddOptions<AzureAdOptions>()
                .Bind(context.Configuration)
                .ValidateDataAnnotations();

        builder.Services.AddOptions<ManagedIdentityOptions>()
                .Bind(context.Configuration)
                .ValidateDataAnnotations();

        // Add Services
        builder.Services.Replace(ServiceDescriptor.Transient(typeof(IOptionsFactory<>), typeof(OptionsFactory<>)));

        builder.Services.AddHttpClient();

        builder.Services.AddSingleton<ITelemetryInitializer, ApplicationVersionInitializer<Startup>>();

        builder.Services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<KerbeeOptions>>();

            return AzureEnvironment.Get(options.Value.Environment);
        });

        builder.Services.AddSingleton<TokenCredential>(provider =>
        {
            var environment = provider.GetRequiredService<AzureEnvironment>();

            return new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                AuthorityHost = environment.AuthorityHost
            });
        });

        builder.Services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<KerbeeOptions>>();
            var credential = provider.GetRequiredService<TokenCredential>();

            return new CertificateClient(new Uri(options.Value.VaultBaseUrl), credential);
        });

        builder.Services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<KerbeeOptions>>();
            var credential = provider.GetRequiredService<TokenCredential>();

            return new SecretClient(new Uri(options.Value.VaultBaseUrl), credential);
        });

        builder.Services.AddSingleton<WebhookInvoker>();
        builder.Services.AddSingleton<Microsoft.Azure.WebJobs.Extensions.DurableTask.ILifeCycleNotificationHelper, WebhookLifeCycleNotification>();

        builder.Services.AddScoped<GraphClientService>();
        builder.Services.AddScoped<IApplicationService, ApplicationService>();
    }
}
