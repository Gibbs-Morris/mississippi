#pragma warning disable CS0618 // Testing legacy composition APIs pending issue #237.
using Microsoft.Extensions.DependencyInjection;

using Mississippi.Brooks.Abstractions.Streaming;
using Mississippi.Brooks.Runtime;
using Mississippi.Brooks.Runtime.Storage.Abstractions;
using Mississippi.Testing.Utilities.Storage;

using Orleans.Hosting;
using Orleans.TestingHost;


namespace Mississippi.DomainModeling.Runtime.L0Tests.Infrastructure;

/// <summary>
///     Silo configuration for the UxProjections test cluster.
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
            services.AddUxProjections();
            services.AddEventSourcingByService(); // Registers IStreamIdFactory
            services.AddSingleton<InMemoryBrookStorage>();
            services.AddSingleton<IBrookStorageReader>(sp => sp.GetRequiredService<InMemoryBrookStorage>());
        });

        // Required for memory streams pub/sub validation
        siloBuilder.AddMemoryGrainStorage("PubSubStore");
    }
}

#pragma warning restore CS0618