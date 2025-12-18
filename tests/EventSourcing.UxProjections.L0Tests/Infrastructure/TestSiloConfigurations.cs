using Microsoft.Extensions.DependencyInjection;

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
            services.AddSingleton<IStreamIdFactory, StreamIdFactory>();
        });

        // Required for memory streams pub/sub validation
        siloBuilder.AddMemoryGrainStorage("PubSubStore");
    }
}