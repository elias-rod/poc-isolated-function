using Azure.Messaging.EventGrid;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

class PocOrchestration
{
    private readonly ServiceBusClient _serviceBusClient;
    private readonly CosmosClient _cosmosClient;
    private readonly EventGridPublisherClient _eventGridPublisherClient;
    private readonly PocConfig _pocConfig;

    public PocOrchestration(ServiceBusClient serviceBusClient, CosmosClient cosmosClient, EventGridPublisherClient eventGridPublisherClient, IOptionsSnapshot<PocConfig> optionsSnapshot)
    {
        _serviceBusClient = serviceBusClient;
        _cosmosClient = cosmosClient;
        _eventGridPublisherClient = eventGridPublisherClient;
        _pocConfig = optionsSnapshot.Value;
    }

    [Function(nameof(PocOrchestrationAsync))]
    public async Task<string> PocOrchestrationAsync([OrchestrationTrigger] TaskOrchestrationContext context, FunctionContext executionContext)
    {
        var cosmosDocId = await context.CallActivityAsync<string>(nameof(PocCosmosActivityAsync), context.InstanceId);
        await context.CallActivityAsync(nameof(PocServiceBusActivityAsync), context.InstanceId);
        await context.WaitForExternalEvent<string>("ExternalEventAlert");
        
        var durableLogger = context.CreateReplaySafeLogger(executionContext.GetLogger(nameof(PocOrchestrationAsync))); //This is going to change in the nuget future realease to be just context.CreateReplaySafeLogger()
        durableLogger.LogInformation("Woked orchestration {InstanceId}", context.InstanceId);

        await context.CallActivityAsync(
            nameof(PocEventGridActivityAsync),
            new PocEventGridCommand { InstanceId = context.InstanceId, PocCosmosDocumentId = cosmosDocId }
        );

        durableLogger.LogInformation("Finished orchestration {InstanceId}", context.InstanceId);

        return cosmosDocId;
    }

    [Function(nameof(PocCosmosActivityAsync))]
    public async Task<string> PocCosmosActivityAsync([ActivityTrigger] string instanceId, FunctionContext executionContext)
    {
        var container = _cosmosClient.GetContainer(_pocConfig.CosmosDatabaseId, _pocConfig.CosmosContainerId);
        var cosmosDocId = Guid.NewGuid().ToString();
        await container.CreateItemAsync(new PocDocument(cosmosDocId, Random.Shared.Next().ToString()), new PartitionKey(cosmosDocId));

        var logger = executionContext.GetLogger(nameof(PocCosmosActivityAsync));
        logger.LogInformation("Document {CosmosDocId} saved in Cosmos for orchestration {InstanceId}", cosmosDocId, instanceId);

        return cosmosDocId;
    }

    [Function(nameof(PocServiceBusActivityAsync))]
    public async Task PocServiceBusActivityAsync([ActivityTrigger] string instanceId, FunctionContext executionContext)
    {
        await using var sender = _serviceBusClient.CreateSender(_pocConfig.ServiceBusQueueName);
        var message = new PocMessage(instanceId, 3);
        await sender.SendMessageAsync(new ServiceBusMessage(JsonSerializer.Serialize(message)));

        var logger = executionContext.GetLogger(nameof(PocServiceBusActivityAsync));
        logger.LogInformation("Message queued in ServiceBus for orchestration {InstanceId}", instanceId);
    }

    [Function(nameof(PocEventGridActivityAsync))]
    public async Task PocEventGridActivityAsync([ActivityTrigger] PocEventGridCommand command, FunctionContext executionContext)
    {
        var response = await _eventGridPublisherClient.SendEventAsync(new EventGridEvent(nameof(PocEvent), nameof(PocEvent), "1.0", new PocEvent(command.InstanceId, command.PocCosmosDocumentId)));

        var logger = executionContext.GetLogger(nameof(PocEventGridActivityAsync));
        logger.LogInformation("Event published to EventGrid for orchestration {InstanceId}", command.InstanceId);
    }
}
