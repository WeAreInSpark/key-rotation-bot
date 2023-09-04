using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Kerbee.Models;

namespace Kerbee.Graph;

public interface IApplicationService
{
    Task AddApplicationAsync(Application application, bool addOwner);
    Task DeleteApplicationAsync(Application application);
    Task<IEnumerable<Application>> GetApplicationsAsync(DateTime? expiryDate = null);
    Task UpdateApplications();
    Task UnmanageApplicationAsync(string applicationId);
    Task RenewKeyAsync(string applicationId, bool replaceCurrent = false);
    Task RenewKeyAsync(Application application, bool replaceCurrent = false);
    Task RenewCertificate(Application application, bool replaceCurrent);
    Task RenewSecret(Application application, bool replaceCurrent);
    Task RemoveKeyAsync(Application application);
    Task RemoveKeyAsync(string applicationId);
    Task PurgeKeys(Application application);
}
