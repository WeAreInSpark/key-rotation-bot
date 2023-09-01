using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Kerbee.Models;

namespace Kerbee.Graph;

public interface IApplicationService
{
    Task AddApplicationAsync(Application application, bool addOwner);
    Task DeleteApplicationAsync(Application application);
    Task RenewCertificate(Application application);
    Task RenewSecret(Application application);
    Task UpdateApplications();
    Task RenewKeyAsync(Application application);
    Task<IEnumerable<Application>> GetApplicationsAsync(DateTime? expiryDate = null);
    Task UnmanageApplicationAsync(string applicationId);
    Task RenewKeyAsync(string applicationId);
    Task RemoveKeyAsync(Application application);
    Task RemoveKeyAsync(string applicationId);
    Task PurgeKeys(Application application);
}
