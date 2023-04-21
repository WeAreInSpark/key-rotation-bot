using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Kerbee.Models;

namespace Kerbee.Functions;

public interface ISharedActivity
{
    Task<IReadOnlyList<CertificateItem>> GetExpiringCertificates(DateTime currentDateTime);

    Task<IReadOnlyList<CertificateItem>> GetAllCertificates(object input = null);

    Task RevokeCertificate(string certificateName);

    Task SendCompletedEvent((string, DateTimeOffset?, IReadOnlyList<string>) input);
}
