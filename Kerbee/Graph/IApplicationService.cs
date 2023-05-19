using System.Collections.Generic;
using System.Threading.Tasks;

using Kerbee.Models;

namespace Kerbee.Graph;

public interface IApplicationService
{
    Task<IEnumerable<Application>> GetApplicationsAsync();
    Task AddApplicationAsync(Application application);
    Task DeleteApplicationAsync(Application application);
}
