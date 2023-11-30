using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using Azure.Identity;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kerbee.Internal.Fakes;

public class FakeClaimsPrincipalMiddleware : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        if (context.FunctionDefinition.InputBindings.Values.Any(x => x.Type == "httpTrigger"))
        {
            var accessor = context.InstanceServices.GetRequiredService<IClaimsPrincipalAccessor>();

            if (accessor.AccessToken is null)
            {
                accessor.Principal = new ClaimsPrincipal(new ClaimsIdentity("fake"));

                var scopes = new string[] { "https://graph.microsoft.com" };
                var accessToken = await new DefaultAzureCredential().GetTokenAsync(new(scopes));
                accessor.AccessToken = accessToken.Token;
            }
        }

        await next(context);
    }
}
