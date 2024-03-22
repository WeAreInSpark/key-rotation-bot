using System.Threading.Tasks;

using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace Kerbee.Graph;
public interface IManagedIdentityProvider
{
    Task<ServicePrincipal> GetAsync();
    GraphServiceClient GetClient();
}
