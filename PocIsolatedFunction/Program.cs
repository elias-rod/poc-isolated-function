using Azure.Identity;
using Microsoft.Extensions.Azure;
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
    })
    .Build();

host.Run();
