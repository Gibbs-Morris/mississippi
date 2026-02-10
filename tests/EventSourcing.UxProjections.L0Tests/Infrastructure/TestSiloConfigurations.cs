using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Abstractions;
using Mississippi.Common.Abstractions.Builders;
using Mississippi.EventSourcing.Brooks;
using Mississippi.EventSourcing.Brooks.Abstractions.Storage;
using Mississippi.Sdk.Silo;
using Mississippi.Testing.Utilities.Storage;

using Orleans.Hosting;
using Orleans.TestingHost;


namespace Mississippi.EventSourcing.UxProjections.L0Tests.Infrastructure;

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
        IMississippiSiloBuilder mississippi = siloBuilder.AddMississippiSilo();

        // Host configures stream infrastructure
        siloBuilder.AddMemoryStreams(MississippiDefaults.StreamProviderName);

        // Tell Brooks which stream provider to use
        mississippi.AddEventSourcing();
        mississippi.AddUxProjections();
        mississippi.ConfigureServices(services =>
        {
            services.AddSingleton<InMemoryBrookStorage>();
            services.AddSingleton<IBrookStorageReader>(sp => sp.GetRequiredService<InMemoryBrookStorage>());
        });

        // Required for memory streams pub/sub validation
        siloBuilder.AddMemoryGrainStorage("PubSubStore");
    }
}