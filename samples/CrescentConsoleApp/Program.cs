// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mississippi.Core.Projection;
using Mississippi.CrescentConsoleApp.Grains;
using Orleans.Configuration;

var builder = Host.CreateApplicationBuilder(args);
builder.UseOrleans(silo =>
{
    silo.UseLocalhostClustering()
        .Configure<ClusterOptions>(opt =>
        {
            opt.ClusterId = "dev";
            opt.ServiceId = "SampleApp";
        });
});
builder.Logging.AddConsole();
using var host = builder.Build();
await host.StartAsync();
try
{
    var client = host.Services.GetRequiredService<IClusterClient>();
    var grain = client.GetGrain<ISampleGrain>(0);
    var reply = await grain.HelloWorldAsync();
    Console.WriteLine($"Grain says: {reply}");
    Console.WriteLine("Press <enter> to exit…");
    var projectionGrain = client.GetGrain<ITestGrain>("ModelA");
    var sample = await projectionGrain.GetAsync();
    Console.WriteLine(sample);

    var projectionGrain1 = client.GetGrain<IProjectionGrain<ModelA>>("ModelB");
    var sample1 = await projectionGrain1.GetAsync();
    Console.WriteLine(sample1);
}
catch (Exception e)
{
    Console.WriteLine("An error occurred while running the sample:");
    Console.WriteLine(e);
    throw;
}

Console.ReadLine();
await host.StopAsync();