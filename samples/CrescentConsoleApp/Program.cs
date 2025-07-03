// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Mississippi.CrescentConsoleApp.Grains;

using Orleans.Configuration;


HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
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
using IHost host = builder.Build();
await host.StartAsync().ConfigureAwait(true);
IClusterClient client = host.Services.GetRequiredService<IClusterClient>();
ISampleGrain? grain = client.GetGrain<ISampleGrain>(0);
string reply = await grain.HelloWorldAsync().ConfigureAwait(true);
Console.WriteLine($"Grain says: {reply}");
Console.WriteLine("Press <enter> to exit…");
Console.ReadLine();
await host.StopAsync().ConfigureAwait(true);