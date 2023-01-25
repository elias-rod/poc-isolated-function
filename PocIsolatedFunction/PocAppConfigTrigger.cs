using Azure.Messaging.EventGrid;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration.Extensions;
using Microsoft.Extensions.Logging;

class PocAppConfigTrigger
{
    private readonly IConfigurationRefresher _configurationRefresher;

    public PocAppConfigTrigger(IConfigurationRefresher configurationRefresher)
    {
        _configurationRefresher = configurationRefresher;
    }

    [Function(nameof(PocAppConfigTriggerAsync))]
    public async Task PocAppConfigTriggerAsync(
        [EventGridTrigger] EventGridEvent eventGridEvent,
        FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger(nameof(PocAppConfigTriggerAsync));
        logger.LogInformation("Received Azure AppConfiguration event {EventData}", eventGridEvent.Data);

        eventGridEvent.TryCreatePushNotification(out PushNotification pushNotification);
        _configurationRefresher.ProcessPushNotification(pushNotification, TimeSpan.FromSeconds(0));
        await _configurationRefresher.RefreshAsync();

        logger.LogInformation("Azure AppConfiguration keys refreshed triggered by {ResourceUri}", pushNotification.ResourceUri);
    }
}