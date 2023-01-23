using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

class PocServiceBusTrigger
{
    [Function(nameof(PocServiceBusTriggerAsync))]
    public static async Task PocServiceBusTriggerAsync(
        [ServiceBusTrigger("sbq-pocif-dev-bs2-1")] PocMessage message,
        FunctionContext executionContext,
        [DurableClient] DurableClientContext durableClientContext)
    {
        var logger = executionContext.GetLogger(nameof(PocServiceBusTriggerAsync));
        logger.LogInformation("Received message {Message}", message);

        await Task.Delay(message.Seconds);
        await durableClientContext.Client.RaiseEventAsync(message.Id, "ExternalEventAlert", null);
        logger.LogInformation("Waking up orchestration {InstanceId}", message.Id);
    }
}
