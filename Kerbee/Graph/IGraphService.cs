using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Graph.Models;

namespace Kerbee.Graph;

public interface IGraphService
{
    Task MakeManagedIdentityOwnerOfApplicationAsync(string applicationObjectId);
    Task<IEnumerable<Application>> GetApplicationsAsync();
    Task<IEnumerable<Application>> GetUnmanagedApplicationsAsync();
    Task<PasswordCredential> GenerateSecretAsync(string applicationObjectId, int ValidityInMonths);
    Task AddCertificateAsync(string applicationObjectId, byte[] cer, params string[] keysToReplace);
    Task RemoveManagedIdentityAsOwnerOfApplicationAsync(string applicationObjectId);
    Task RemoveCertificateAsync(string applicationObjectId, string keyId, ClientType clientType);
    Task RemoveSecretAsync(string applicationObjectId, Guid keyId, ClientType clientType);
    Task<Application?> GetApplicationAsync(string applicationObjectId);
}
