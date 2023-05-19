using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Graph.Models;

namespace Kerbee.Graph;

public interface IGraphService
{
    Task MakeManagedIdentityOwnerOfApplicationAsync(Application application);
    Task<IEnumerable<Application>> GetApplicationsAsync();
    Task<IEnumerable<Application>> GetUnmanagedApplicationsAsync();
}
