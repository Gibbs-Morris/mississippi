using Microsoft.Extensions.DependencyInjection;

using Mississippi.EventSourcing.Brooks;
using Mississippi.EventSourcing.Brooks.Abstractions.Storage;
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
        siloBuilder.AddMemoryStreams("MississippiBrookStreamProvider");
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