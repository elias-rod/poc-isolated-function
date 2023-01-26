using Azure.Messaging.EventGrid;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

class PocEventGridTrigger
{
    private readonly CosmosClient _cosmosClient;
    private readonly PocConfig _pocConfig;

    public PocEventGridTrigger(CosmosClient cosmosClient, IOptionsSnapshot<PocConfig> optionsSnapshot)
    {
        _cosmosClient = cosmosClient;
        _pocConfig = optionsSnapshot.Value;
    }

    [Function(nameof(PocEventGridTriggerAsync))]
    public async Task PocEventGridTriggerAsync(
        [EventGridTrigger] EventGridEvent eventGridEvent,
        FunctionContext functionContext,
        CancellationToken cancellationToken)
    {
        var logger = functionContext.GetLogger(nameof(PocEventGridTriggerAsync));
        logger.LogInformation("Received event {EventData}", eventGridEvent.Data);

        var pocEvent = eventGridEvent.Data.ToObjectFromJson<PocEvent>();

        var container = _cosmosClient.GetContainer(_pocConfig.CosmosDatabaseId, _pocConfig.CosmosContainerId);
        var itemResponse = await container.ReadItemAsync<PocDocument>(
            pocEvent.PocDocumentId,
            new PartitionKey(pocEvent.PocDocumentId),
            cancellationToken: cancellationToken);

        logger.LogInformation(
            "Read Cosmos document {PocDocumentId} named {PocDocumentName} for instance {InstanceId}",
            pocEvent.PocDocumentId,
            itemResponse.Resource.Name,
            pocEvent.InstanceId);
    }
}
