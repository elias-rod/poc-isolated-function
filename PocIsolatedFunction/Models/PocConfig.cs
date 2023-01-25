public class PocConfig
{
    public string? ServiceBusEndpoint { get; set; }
    public Uri? EventGridEndpoint { get; set; }
    public string? CosmosEndpoint { get; set; }
    public Uri? AppConfigurationEndpoint { get; set; }
    public string? ServiceBusQueueName { get; set; }
    public string? CosmosDatabaseId { get; set; }
    public string? CosmosContainerId { get; set; }
    public int MessageDelayInSeconds { get; set; }
    public string? Prefix { get; set; }
}
