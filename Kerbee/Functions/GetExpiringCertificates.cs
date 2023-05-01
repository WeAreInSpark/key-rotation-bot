using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Azure.Security.KeyVault.Certificates;

using Kerbee.Internal;
using Kerbee.Models;
using Kerbee.Options;

using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kerbee.Functions;

[DurableTask(nameof(GetExpiringCertificates))]
public class GetExpiringCertificates : TaskActivity<DateTime, IReadOnlyList<CertificateItem>>
{
    public GetExpiringCertificates(CertificateClient certificateClient,
                          WebhookInvoker webhookInvoker, IOptions<KerbeeOptions> options, ILogger<GetExpiringCertificates> logger)
    {
        _certificateClient = certificateClient;
        _webhookInvoker = webhookInvoker;
        _options = options.Value;
        _logger = logger;
    }

    private readonly CertificateClient _certificateClient;
    private readonly WebhookInvoker _webhookInvoker;
    private readonly KerbeeOptions _options;
    private readonly ILogger<GetExpiringCertificates> _logger;

    private const string IssuerName = "Kerbee";

    public override async Task<IReadOnlyList<CertificateItem>> RunAsync(TaskActivityContext context, DateTime currentDateTime)
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
}
