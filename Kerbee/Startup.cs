using System;
using System.Linq;

using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;

using Kerbee.Internal;
using Kerbee.Options;

using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Identity.Web;

[assembly: FunctionsStartup(typeof(Kerbee.Startup))]

namespace Kerbee;

public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        var context = builder.GetContext();

        // Add Options
        builder.Services.AddOptions<KerbeeOptions>()
               .Bind(context.Configuration.GetSection("Kerbee"))
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
        builder.Services.AddSingleton<ILifeCycleNotificationHelper, WebhookLifeCycleNotification>();
    }
}
