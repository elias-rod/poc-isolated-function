using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

IConfigurationRefresher configurationRefresher = null!;
var defaultAzureCredential = new DefaultAzureCredential();

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration((hostBuilderContext, configurationBuilder) =>
    {
        var pocConfig = configurationBuilder.Build().Get<PocConfig>();
        configurationBuilder.AddAzureAppConfiguration(azureAppConfigurationOptions =>
        {
            configurationRefresher = azureAppConfigurationOptions
                .Connect(pocConfig!.AppConfigurationEndpoint, defaultAzureCredential)
                .Select($"{PocConstant.Prefix}:{PocConstant.AppConfigWildcard}")
                .TrimKeyPrefix($"{PocConstant.Prefix}:")
                .ConfigureRefresh(azureAppConfigurationRefreshOptions =>
                    azureAppConfigurationRefreshOptions
                        .Register($"{PocConstant.Prefix}:{PocConstant.SentinelKey}", refreshAll: true)
                        .SetCacheExpiration(TimeSpan.FromDays(1))
                )
                .GetRefresher();
        });
    })
    .ConfigureServices((hostBuilderContext, serviceCollection) =>
    {
        var pocConfig = hostBuilderContext.Configuration.Get<PocConfig>();
        serviceCollection.Configure<PocConfig>(hostBuilderContext.Configuration);
        serviceCollection.AddAzureClients(azureClientFactoryBuilder =>
        {
            azureClientFactoryBuilder.AddServiceBusClientWithNamespace(pocConfig!.ServiceBusEndpoint);
            azureClientFactoryBuilder.AddEventGridPublisherClient(pocConfig.EventGridEndpoint);
            azureClientFactoryBuilder.UseCredential(defaultAzureCredential);
        });
        serviceCollection.AddSingleton(new CosmosClient(
            pocConfig!.CosmosEndpoint,
            defaultAzureCredential,
            new CosmosClientOptions { SerializerOptions = new CosmosSerializationOptions { PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase } }
        ));
        serviceCollection.AddSingleton(configurationRefresher);
    })
    .Build();

host.Run();
