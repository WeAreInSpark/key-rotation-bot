using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Kerbee.Graph;
using Kerbee.Models;
using Kerbee.Options;

using Microsoft.DurableTask;
using Microsoft.Extensions.Options;

namespace Kerbee.Functions;

[DurableTask(nameof(GetExpiringCertificatesAndSecrets))]
public class GetExpiringCertificatesAndSecrets : TaskActivity<DateTime, IEnumerable<Application>>
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

    public override async Task<IEnumerable<Application>> RunAsync(TaskActivityContext context, DateTime expiryDate)
    {
        return await _applicationService.GetApplicationsAsync(expiryDate);
    }
}

