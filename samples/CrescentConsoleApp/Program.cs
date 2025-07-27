// See https://aka.ms/new-console-template for more information

using System.Collections.Immutable;
using System.Security.Cryptography;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Mississippi.Core.Abstractions.Brooks;
using Mississippi.Core.Brooks.Grains.Factory;
using Mississippi.Core.Brooks.Grains.Reader;
using Mississippi.Core.Brooks.Grains.Writer;
using Mississippi.CrescentConsoleApp;

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

namespace Mississippi.CrescentConsoleApp
{
    public static class SampleEventFactory
    {
        private static readonly string[] MimeTypes =
        {
            "application/json", "text/plain", "application/xml", "application/octet-stream",
        };

        private static readonly string[] Categories = { "Metric", "Alert", "Heartbeat", "Audit", "Diagnostic" };

        private static readonly Random Rng = new(); // single RNG instance

        public static ImmutableArray<BrookEvent> CreateEvents(
            int count = 10
        )
        {
            ImmutableArray<BrookEvent>.Builder builder = ImmutableArray.CreateBuilder<BrookEvent>(count);
            for (int i = 0; i < count; i++)
            {
                builder.Add(CreateRandomEvent());
            }

            return builder.MoveToImmutable(); // O(1) finalise
        }

        private static BrookEvent CreateRandomEvent()
        {
            byte[] payload = new byte[Rng.Next(512, 4_096)];
            RandomNumberGenerator.Fill(payload);
            return new()
            {
                Id = Guid.NewGuid().ToString(),
                Data = payload.ToImmutableArray(),
                DataContentType = Pick(MimeTypes),
                Source = GenerateSourceTag(),
                Time = DateTimeOffset.UtcNow.AddSeconds(-Rng.Next(0, 86_400)),
                Type = Pick(Categories),
            };
        }

        private static string GenerateSourceTag()
        {
            const string pool = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            Span<char> tag = stackalloc char[5];
            for (int i = 0; i < tag.Length; i++)
            {
                tag[i] = pool[Rng.Next(pool.Length)];
            }

            return $"SRC-{new string(tag)}";
        }

        private static T Pick<T>(
            T[] array
        ) =>
            array[Rng.Next(array.Length)];
    }
}