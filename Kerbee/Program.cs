using System;
using System.Text.Json;
using System.Text.Json.Serialization;

using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;

using Kerbee;
using Kerbee.Graph;
using Kerbee.Internal;
using Kerbee.Internal.Fakes;
using Kerbee.Options;

using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(builder =>
    {
        builder
            .AddApplicationInsights()
            .AddApplicationInsightsLogger();

        builder.UseWhen<ClaimsPrincipalMiddleware>(context =>
        {
            var options = context.InstanceServices.GetRequiredService<IOptions<DeveloperOptions>>();
            return !options.Value.UseFakeAuth;
        });

        builder.UseWhen<FakeClaimsPrincipalMiddleware>(context =>
        {
            var options = context.InstanceServices.GetRequiredService<IOptions<DeveloperOptions>>();
            return options.Value.UseFakeAuth;
        });
    })
    .ConfigureLogging(builder =>
    {
        builder.AddConsole();
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

        services.AddOptions<WebsiteOptions>()
                .Bind(context.Configuration)
                .ValidateDataAnnotations();

        services.AddOptions<DeveloperOptions>()
                .Bind(context.Configuration.GetSection(DeveloperOptions.Developer))
                .ValidateDataAnnotations();

        // Add Services
        services.Configure<JsonSerializerOptions>(options =>
        {
            options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.Converters.Add(new JsonStringEnumConverter());
        });

        services.AddSingleton<IClaimsPrincipalAccessor, ClaimsPrincipalAccessor>();

        services.AddHttpClient();

        services.AddSingleton<ITelemetryInitializer, ApplicationVersionInitializer<Program>>();

        services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<KerbeeOptions>>();

            return AzureEnvironment.Get(options.Value.Environment);
        });

        services.AddSingleton(provider =>
        {
            var environment = provider.GetRequiredService<AzureEnvironment>();
            var options = provider.GetRequiredService<IOptions<KerbeeOptions>>();
            var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                AuthorityHost = environment.AuthorityHost
            });

            return new CertificateClient(new Uri(options.Value.VaultBaseUrl), credential);
        });

        services.AddSingleton(provider =>
        {
            var environment = provider.GetRequiredService<AzureEnvironment>();
            var options = provider.GetRequiredService<IOptions<KerbeeOptions>>();
            var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                AuthorityHost = environment.AuthorityHost
            });

            return new SecretClient(new Uri(options.Value.VaultBaseUrl), credential);
        });

        services.AddSingleton<IClaimsPrincipalAccessor>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<DeveloperOptions>>();
            return options.Value.UseFakeAuth
                ? new FakeClaimsPrincipalAccessor()
                : new ClaimsPrincipalAccessor();
        });

        services.AddSingleton<WebhookInvoker>();
        services.AddSingleton<ILifecycleNotificationHelper, WebhookLifeCycleNotification>();
        services.AddSingleton<ManagedIdentityProvider>();

        services.AddScoped<IGraphService, GraphService>();

        services.AddScoped<IApplicationService, ApplicationService>();
    })
    .Build();

await host.RunAsync();
