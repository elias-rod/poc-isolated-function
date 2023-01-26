using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

class PocServiceBusTrigger
{
    [Function(nameof(PocServiceBusTriggerAsync))]
    public static async Task PocServiceBusTriggerAsync(
        [ServiceBusTrigger(PocConstant.ServiceBusTriggerQueueName)] PocMessage message,
        FunctionContext functionContext,
        [DurableClient] DurableClientContext durableClientContext,
        CancellationToken cancellationToken)
    {
        var logger = functionContext.GetLogger(nameof(PocServiceBusTriggerAsync));
        logger.LogInformation("Received message {Message}", message);

        await Task.Delay(message.Seconds, cancellationToken);
        await durableClientContext.Client.RaiseEventAsync(message.Id, "ExternalEventAlert", null);

        logger.LogInformation("Waking up orchestration {InstanceId}", message.Id);
    }
}
