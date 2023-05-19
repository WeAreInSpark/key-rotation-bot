using System.Security.Claims;
using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;

namespace Kerbee.Internal.Fakes;

public class FakeClaimsPrincipalMiddleware : IFunctionsWorkerMiddleware
{
    public FakeClaimsPrincipalMiddleware()
    {
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var accessor = context.InstanceServices.GetRequiredService<IClaimsPrincipalAccessor>();

        accessor.Principal = new ClaimsPrincipal(new ClaimsIdentity("fake"));

        await next(context);
    }
}
