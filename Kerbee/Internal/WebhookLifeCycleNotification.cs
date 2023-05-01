using System.Threading.Tasks;

namespace Kerbee.Internal;

internal class WebhookLifeCycleNotification : ILifecycleNotificationHelper
{
    public WebhookLifeCycleNotification(WebhookInvoker webhookInvoker)
    {
        _webhookInvoker = webhookInvoker;
    }

    private readonly WebhookInvoker _webhookInvoker;

    public Task OrchestratorStartingAsync(string hubName, string functionName, string instanceId, bool isReplay) => Task.CompletedTask;

    public Task OrchestratorCompletedAsync(string hubName, string functionName, string instanceId, bool continuedAsNew, bool isReplay) => Task.CompletedTask;

    public async Task OrchestratorFailedAsync(string hubName, string functionName, string instanceId, string reason, bool isReplay)
    {
        await _webhookInvoker.SendFailedEventAsync(functionName, reason);
    }

    public Task OrchestratorTerminatedAsync(string hubName, string functionName, string instanceId, string reason) => Task.CompletedTask;
}
