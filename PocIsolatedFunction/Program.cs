using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Azure;
using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Azure.Messaging.EventGrid;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((hostBuilderContext, serviceCollection) =>
    {
        serviceCollection.AddAzureClients(azureClientFactoryBuilder =>
        {
            azureClientFactoryBuilder.AddServiceBusClientWithNamespace("sb-pocif-dev-bs2-1.servicebus.windows.net");
            azureClientFactoryBuilder.AddEventGridPublisherClient(new Uri("https://evgt-pocif-dev-bs2-1.brazilsouth-1.eventgrid.azure.net/api/events"));
            azureClientFactoryBuilder.UseCredential(new DefaultAzureCredential());
        });
        serviceCollection.AddSingleton(new CosmosClient(
            "https://cosmos-pocif-dev-bs2-1.documents.azure.com:443/",
            new DefaultAzureCredential(),
            new CosmosClientOptions { SerializerOptions = new CosmosSerializationOptions { PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase } }
        ));
    })
    .Build();

host.Run();
