#pragma warning disable CS0618 // Testing legacy composition APIs pending issue #237.
using Microsoft.Extensions.DependencyInjection;

using Mississippi.Brooks.Abstractions.Streaming;
using Mississippi.Brooks.Runtime.Storage.Abstractions;
using Mississippi.Testing.Utilities.Storage;

using Orleans.Hosting;
using Orleans.TestingHost;


namespace Mississippi.Brooks.Runtime.L0Tests.Infrastructure;

/// <summary>
///     Silo configuration for the EventSourcing test cluster.
/// </summary>
internal sealed class TestSiloConfigurations : ISiloConfigurator
{
    /// <inheritdoc />
    public void Configure(
        ISiloBuilder siloBuilder
    )
    {
        // Host configures stream infrastructure
        siloBuilder.AddMemoryStreams(BrookStreamingDefaults.OrleansStreamProviderName);

        // Tell Brooks which stream provider to use
        siloBuilder.AddEventSourcing();
        siloBuilder.ConfigureServices(services =>
        {
            services.AddEventSourcingByService();
            services.AddSingleton<InMemoryBrookStorage>();
            services.AddSingleton<IBrookStorageReader>(sp => sp.GetRequiredService<InMemoryBrookStorage>());
            services.AddSingleton<IBrookStorageWriter>(sp => sp.GetRequiredService<InMemoryBrookStorage>());
        });

        // Required for memory streams pub/sub validation
        siloBuilder.AddMemoryGrainStorage("PubSubStore");
    }
}

#pragma warning restore CS0618