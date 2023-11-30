using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;

namespace Kerbee.Internal;

public class AzureEventSourceLogForwarderMiddleware(ILoggerFactory loggerFactory) : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        using var logger = new AzureEventSourceLogForwarder(loggerFactory);
        logger.Start();

        await next(context);
    }
}
