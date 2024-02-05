using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Kerbee.Graph;
using Kerbee.Models;
using Kerbee.Options;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Options;

namespace Kerbee.Functions;

public class GetExpiringCertificatesAndSecrets
{
    public GetExpiringCertificatesAndSecrets(
        IApplicationService applicationService,
        IOptions<KerbeeOptions> options)
    {
        _applicationService = applicationService;
        _options = options.Value;
    }

    private readonly IApplicationService _applicationService;
    private readonly KerbeeOptions _options;

    [Function(nameof(GetExpiringCertificatesAndSecrets))]
    public async Task<IEnumerable<Application>> RunAsync([ActivityTrigger] DateTime expiryDate)
    {
        return await _applicationService.GetApplicationsAsync(expiryDate);
    }
}

