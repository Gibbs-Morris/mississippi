// See https://aka.ms/new-console-template for more information

using CrescentConsoleApp.Grains;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Orleans.Configuration;


HostApplicationBuilder
    builder = Host.CreateApplicationBuilder(args); // .NET 9 preferred entry point :contentReference[oaicite:0]{index=0}
builder.UseOrleans(silo =>
{
    silo.UseLocalhostClustering() // quick, in-proc dev cluster :contentReference[oaicite:1]{index=1}
        .Configure<ClusterOptions>(opt =>
        {
            opt.ClusterId = "dev";
            opt.ServiceId = "SampleApp"; // basic identity settings :contentReference[oaicite:2]{index=2}
        });
});
builder.Logging.AddConsole();
using IHost host = builder.Build();
await host.StartAsync().ConfigureAwait(true); // silo + DI container now running
IClusterClient
    client = host.Services
        .GetRequiredService<IClusterClient>(); // auto-registered with UseOrleans :contentReference[oaicite:3]{index=3}
ISampleGrain? grain = client.GetGrain<ISampleGrain>(0);
string reply = await grain.HelloWorldAsync().ConfigureAwait(true);
Console.WriteLine($"Grain says: {reply}");
Console.WriteLine("Press <enter> to exit…");
Console.ReadLine();
await host.StopAsync().ConfigureAwait(true);