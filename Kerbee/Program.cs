using System;

using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;

using Kerbee.Graph;
using Kerbee.Internal;
using Kerbee.Options;

using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(builder =>
    {
        builder
            .AddApplicationInsights()
            .AddApplicationInsightsLogger();
    })
    .ConfigureServices((context, services) =>
    {
        // Add Options
        services.AddOptions<KerbeeOptions>()
               .Bind(context.Configuration.GetSection(KerbeeOptions.Kerbee))
               .ValidateDataAnnotations();

        services.AddOptions<AzureAdOptions>()
                .Bind(context.Configuration)
                .ValidateDataAnnotations();

        services.AddOptions<ManagedIdentityOptions>()
                .Bind(context.Configuration)
                .ValidateDataAnnotations();

        // Add Services
        services.AddHttpContextAccessor();

        services.AddHttpClient();

        services.AddSingleton<ITelemetryInitializer, ApplicationVersionInitializer<Program>>();

        services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<KerbeeOptions>>();

            return AzureEnvironment.Get(options.Value.Environment);
        });

        services.AddSingleton<TokenCredential>(provider =>
        {
            var environment = provider.GetRequiredService<AzureEnvironment>();

            return new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                AuthorityHost = environment.AuthorityHost
            });
        });

        services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<KerbeeOptions>>();
            var credential = provider.GetRequiredService<TokenCredential>();

            return new CertificateClient(new Uri(options.Value.VaultBaseUrl), credential);
        });

        services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<KerbeeOptions>>();
            var credential = provider.GetRequiredService<TokenCredential>();

            return new SecretClient(new Uri(options.Value.VaultBaseUrl), credential);
        });

        services.AddSingleton<WebhookInvoker>();
        services.AddSingleton<ILifecycleNotificationHelper, WebhookLifeCycleNotification>();

        services.AddScoped<GraphClientService>();
        services.AddScoped<IApplicationService, ApplicationService>();
    })
    .Build();

await host.RunAsync();
