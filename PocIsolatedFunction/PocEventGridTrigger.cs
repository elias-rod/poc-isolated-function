using Azure.Messaging.EventGrid;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

class PocEventGridTrigger
{
    private readonly CosmosClient _cosmosClient;

    public PocEventGridTrigger(CosmosClient cosmosClient)
    {
        _cosmosClient = cosmosClient;
    }

    [Function(nameof(PocEventGridTriggerAsync))]
    public async Task PocEventGridTriggerAsync(
        [EventGridTrigger] EventGridEvent eventGridEvent,
        FunctionContext functionContext)
    {
        var logger = functionContext.GetLogger(nameof(PocEventGridTriggerAsync));
        logger.LogInformation("Received event {EventData}", eventGridEvent.Data);

        var pocEvent = eventGridEvent.Data.ToObjectFromJson<PocEvent>();

        var database = _cosmosClient.GetDatabase(id: "pocif");
        var container = database.GetContainer(id: "samples");
        var itemResponse = await container.ReadItemAsync<PocDocument>(pocEvent.PocDocumentId, new PartitionKey(pocEvent.PocDocumentId));

        logger.LogInformation(
            "Read Cosmos document {PocDocumentId} named {PocDocumentName} for instance {InstanceId}",
            pocEvent.PocDocumentId,
            itemResponse.Resource.Name,
            pocEvent.InstanceId);
    }
}
