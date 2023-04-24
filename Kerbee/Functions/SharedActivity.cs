using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Azure.Security.KeyVault.Certificates;

using Kerbee.Internal;
using Kerbee.Models;
using Kerbee.Options;

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kerbee.Functions;

public class SharedActivity : ISharedActivity
{
    public SharedActivity(CertificateClient certificateClient,
                          WebhookInvoker webhookInvoker, IOptions<KerbeeOptions> options, ILogger<SharedActivity> logger)
    {
        _certificateClient = certificateClient;
        _webhookInvoker = webhookInvoker;
        _options = options.Value;
        _logger = logger;
    }

    private readonly CertificateClient _certificateClient;
    private readonly WebhookInvoker _webhookInvoker;
    private readonly KerbeeOptions _options;
    private readonly ILogger<SharedActivity> _logger;

    private const string IssuerName = "Kerbee";

    [FunctionName(nameof(GetExpiringCertificates))]
    public async Task<IReadOnlyList<CertificateItem>> GetExpiringCertificates([ActivityTrigger] DateTime currentDateTime)
    {
        var certificates = _certificateClient.GetPropertiesOfCertificatesAsync();

        var result = new List<CertificateItem>();

        await foreach (var certificate in certificates)
        {
            if ((certificate.ExpiresOn.Value - currentDateTime).TotalDays > _options.RenewBeforeExpiry)
            {
                continue;
            }

            result.Add((await _certificateClient.GetCertificateAsync(certificate.Name)).Value.ToCertificateItem());
        }

        return result;
    }

    [FunctionName(nameof(GetAllCertificates))]
    public async Task<IReadOnlyList<CertificateItem>> GetAllCertificates([ActivityTrigger] object input = null)
    {
        var certificates = _certificateClient.GetPropertiesOfCertificatesAsync();

        var result = new List<CertificateItem>();

        await foreach (var certificate in certificates)
        {
            var certificateItem = (await _certificateClient.GetCertificateAsync(certificate.Name)).Value.ToCertificateItem();

            result.Add(certificateItem);
        }

        return result;
    }

    [FunctionName(nameof(RevokeCertificate))]
    public async Task RevokeCertificate([ActivityTrigger] string certificateName)
    {
        var response = await _certificateClient.GetCertificateAsync(certificateName);
        return;
    }

    [FunctionName(nameof(SendCompletedEvent))]
    public Task SendCompletedEvent([ActivityTrigger] (string, DateTimeOffset?, IReadOnlyList<string>) input)
    {
        var (certificateName, expirationDate, dnsNames) = input;

        return _webhookInvoker.SendCompletedEventAsync(certificateName, expirationDate, dnsNames);
    }
}
