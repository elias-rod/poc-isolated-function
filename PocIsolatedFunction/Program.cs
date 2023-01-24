using Azure.Identity;
using Azure.Messaging.EventGrid;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((hostBuilderContext, serviceCollection) =>
    {
        serviceCollection.AddAzureClients(azureClientFactoryBuilder =>
        {
            azureClientFactoryBuilder.AddEventGridPublisherClient(new Uri("https://evgt-pocif-dev-bs2-1.brazilsouth-1.eventgrid.azure.net/api/events"));
            azureClientFactoryBuilder.UseCredential(new DefaultAzureCredential());
        });
        //serviceCollection.AddSingleton(new EventGridPublisherClient(new Uri("https://evgt-pocif-dev-bs2-1.brazilsouth-1.eventgrid.azure.net/api/events"), new DefaultAzureCredential()));
    })
    .Build();

host.Run();
