using Mississippi.Aqueduct.Abstractions;

using Orleans.Hosting;
using Orleans.TestingHost;


namespace Mississippi.Aqueduct.Runtime.L0Tests.Infrastructure;

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
        siloBuilder.AddMemoryStreams(AqueductStreamDefaults.StreamProviderName);

        // Configure memory grain storage for grain state
        siloBuilder.AddMemoryGrainStorage("signalr-grains");
        siloBuilder.AddMemoryGrainStorage("PubSubStore");
    }
}