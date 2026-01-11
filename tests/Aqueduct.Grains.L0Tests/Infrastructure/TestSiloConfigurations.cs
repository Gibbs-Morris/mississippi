using Microsoft.Extensions.DependencyInjection;

using Mississippi.Aqueduct.Abstractions;

using Orleans.Hosting;
using Orleans.TestingHost;


namespace Mississippi.Aqueduct.Grains.L0Tests.Infrastructure;

/// <summary>
///     Silo configuration for the Aqueduct Grains test cluster.
/// </summary>
internal sealed class TestSiloConfigurations : ISiloConfigurator
{
    /// <inheritdoc />
    public void Configure(
        ISiloBuilder siloBuilder
    )
    {
        // Configure memory streams for SignalR backplane
        siloBuilder.AddMemoryStreams("SignalRStreams");

        // Configure memory grain storage for grain state
        siloBuilder.AddMemoryGrainStorage("signalr-grains");
        siloBuilder.AddMemoryGrainStorage("PubSubStore");

        // Configure Aqueduct options
        siloBuilder.ConfigureServices(services =>
        {
            services.Configure<AqueductOptions>(options =>
            {
                options.StreamProviderName = "SignalRStreams";
            });
        });
    }
}