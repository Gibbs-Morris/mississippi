// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Mississippi.CrescentConsoleApp;
using Mississippi.EventSourcing;
using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Cosmos;
using Mississippi.EventSourcing.Factory;
using Mississippi.EventSourcing.Reader;
using Mississippi.EventSourcing.Writer;

using Orleans.Configuration;


HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// Add event sourcing with Orleans configuration
builder.AddEventSourcing();

// Configure Orleans clustering
builder.UseOrleans(silo =>
{
    silo.UseLocalhostClustering()
        .Configure<ClusterOptions>(opt =>
        {
            opt.ClusterId = "dev";
            opt.ServiceId = "SampleApp";
        });
});

// Add Cosmos storage provider with configuration
builder.Services.AddCosmosBrookStorageProvider(
    "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
    "UseDevelopmentStorage=true", // Use Azurite for local development
    options =>
    {
        options.DatabaseId = "mississippi-dev";
        options.QueryBatchSize = 50;
        options.MaxEventsPerBatch = 50;
    });
builder.Logging.AddConsole();
using IHost host = builder.Build();
await host.StartAsync();
try
{
    IClusterClient client = host.Services.GetRequiredService<IClusterClient>();
    IBrookGrainFactory aaa = host.Services.GetRequiredService<IBrookGrainFactory>();
    IBrookWriterGrain writerGrain = aaa.GetBrookWriterGrain(new("x", "x"));
    BrookPosition result = await writerGrain.AppendEventsAsync(SampleEventFactory.CreateEvents(50));
    IBrookReaderGrain readGrain = aaa.GetBrookReaderGrain(new("x", "x"));
    await foreach (BrookEvent mississippiEvent in readGrain.ReadEventsAsync(
                       new BrookPosition(1),
                       new BrookPosition(-1)))
    {
        Console.WriteLine(mississippiEvent);
    }
}
catch (Exception e)
{
    Console.WriteLine("An error occurred while running the sample:");
    Console.WriteLine(e);
    throw;
}

Console.ReadLine();
await host.StopAsync();