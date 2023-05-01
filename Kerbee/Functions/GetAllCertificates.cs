using System.Collections.Generic;
using System.Threading.Tasks;

using Azure.Security.KeyVault.Certificates;

using Kerbee.Internal;
using Kerbee.Models;

using Microsoft.DurableTask;

namespace Kerbee.Functions;

[DurableTask(nameof(GetAllCertificates))]
public class GetAllCertificates : TaskActivity<object, IReadOnlyList<CertificateItem>>
{
    public GetAllCertificates(CertificateClient certificateClient)
    {
        _certificateClient = certificateClient;
    }

    private readonly CertificateClient _certificateClient;

    public override async Task<IReadOnlyList<CertificateItem>> RunAsync(TaskActivityContext context, object input = null)
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
}
