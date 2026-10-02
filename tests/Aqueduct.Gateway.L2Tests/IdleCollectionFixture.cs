using System.Diagnostics;

using Mississippi.Aqueduct.Abstractions;
using Mississippi.Aqueduct.Runtime;
using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Abstractions.Streaming;
using Mississippi.Brooks.Runtime.Storage.Abstractions;
using Mississippi.Hosting.Runtime;
using Mississippi.Inlet.Runtime;
using Mississippi.Inlet.Runtime.Abstractions;

using NSubstitute;

using Orleans.Runtime;


namespace Mississippi.Aqueduct.Gateway.L2Tests;

/// <summary>
///     Owns an isolated silo whose normal collector runs against short-lived routing activations.
/// </summary>
#pragma warning disable CA1515 // xUnit requires a public fixture.
public sealed class IdleCollectionFixture : IAsyncLifetime
#pragma warning restore CA1515
{
    private readonly IHost host;

    /// <summary>
    ///     Initializes a new instance of the <see cref="IdleCollectionFixture" /> class.
    /// </summary>
    public IdleCollectionFixture()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Logging.AddSimpleConsole();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.UseOrleans(silo =>
        {
            silo.UseLocalhostClustering()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "idle-routing-" + Guid.NewGuid().ToString("N");
                    options.ServiceId = "IdleRoutingTests";
                })
                .Configure<GrainCollectionOptions>(options =>
                {
                    options.CollectionQuantum = TimeSpan.FromSeconds(1);
                    TimeSpan collectionAge = TimeSpan.FromSeconds(3);
                    options.ClassSpecificCollectionAge["Mississippi.Aqueduct.Runtime.Grains.SignalRClientGrain"] =
                        collectionAge;
                    options.ClassSpecificCollectionAge["Mississippi.Aqueduct.Runtime.Grains.SignalRGroupGrain"] =
                        collectionAge;
                    options.ClassSpecificCollectionAge["Mississippi.Inlet.Runtime.Grains.InletSubscriptionGrain"] =
                        collectionAge;
                })
                .UseMississippi(runtime => runtime.AddAqueduct(aqueduct => aqueduct.UseMemoryStreams()));
        });
        builder.Services.AddInletSilo();
        builder.Services.Configure<BrookProviderOptions>(options =>
            options.OrleansStreamProviderName = AqueductStreamDefaults.StreamProviderName);
        IBrookStorageReader reader = Substitute.For<IBrookStorageReader>();
        reader.ReadCursorPositionAsync(Arg.Any<BrookKey>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new BrookPosition(0)));
        builder.Services.AddSingleton(reader);
        IStreamIdFactory streamIdFactory = Substitute.For<IStreamIdFactory>();
        streamIdFactory.Create(Arg.Any<BrookKey>())
            .Returns(call => StreamId.Create("idle-brooks", call.Arg<BrookKey>().ToString()));
        builder.Services.AddSingleton(streamIdFactory);
        host = builder.Build();
        host.Services.GetRequiredService<IProjectionBrookRegistry>().Register("idle-projection", "IDLE");
    }

    /// <summary>
    ///     Gets the real client connected to this fixture's silo.
    /// </summary>
    public IClusterClient Client => host.Services.GetRequiredService<IClusterClient>();

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        using (host)
        {
            await host.StopAsync();
        }
    }

    /// <summary>
    ///     Reads catalog statistics without invoking or refreshing the tested grains.
    /// </summary>
    /// <returns>The currently active application grains.</returns>
    public Task<DetailedGrainStatistic[]> GetStatisticsAsync() =>
        Client.GetGrain<IManagementGrain>(0).GetDetailedGrainStatistics();

    /// <inheritdoc />
    public async ValueTask InitializeAsync() => await host.StartAsync();

    /// <summary>
    ///     Waits for an observed activation to leave the catalog through normal or explicit collection.
    /// </summary>
    /// <param name="grainId">The activation's logical identity.</param>
    /// <returns>A task completed only after the activation is absent.</returns>
    public async Task WaitUntilCollectedAsync(
        GrainId grainId
    )
    {
        Stopwatch timeout = Stopwatch.StartNew();
        while (timeout.Elapsed < TimeSpan.FromSeconds(30))
        {
            DetailedGrainStatistic[] statistics = await GetStatisticsAsync();
            if (!statistics.Any(statistic => statistic.GrainId == grainId))
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }

        throw new TimeoutException($"Collector did not release observed grain {grainId}.");
    }
}