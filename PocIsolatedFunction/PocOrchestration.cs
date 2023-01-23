using Azure.Messaging.EventGrid;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

class PocOrchestration
{
    private readonly EventGridPublisherClient _eventGridPublisherClient;

    public PocOrchestration(EventGridPublisherClient eventGridPublisherClient)
    {
        _eventGridPublisherClient = eventGridPublisherClient;
    }

    [Function(nameof(PocOrchestrationAsync))]
    public async Task<string> PocOrchestrationAsync([OrchestrationTrigger] TaskOrchestrationContext context, FunctionContext executionContext)
    {
        await context.CallActivityAsync(
            nameof(PocEventGridActivityAsync),
            new PocEventGridCommand { InstanceId = context.InstanceId, PocCosmosDocumentId = "cosmosDocId" }
        );

        return "cosmosDocId";
    }

    [Function(nameof(PocEventGridActivityAsync))]
    public async Task PocEventGridActivityAsync([ActivityTrigger] PocEventGridCommand command, FunctionContext executionContext)
    {
        var response = await _eventGridPublisherClient.SendEventAsync(new EventGridEvent(nameof(PocEvent), nameof(PocEvent), "1.0", new PocEvent(command.InstanceId, command.PocCosmosDocumentId)));

        var logger = executionContext.GetLogger(nameof(PocEventGridActivityAsync));
        logger.LogInformation("Event published to EventGrid for orchestration {InstanceId}", command.InstanceId);
    }
}
