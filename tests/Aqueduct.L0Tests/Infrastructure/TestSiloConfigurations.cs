using Mississippi.Common.Abstractions;

using Orleans.Hosting;
using Orleans.TestingHost;


namespace Mississippi.Aqueduct.L0Tests.Infrastructure;

/// <summary>
///     Silo configuration for the SignalR Orleans test cluster.
/// </summary>
internal sealed class TestSiloConfigurations : ISiloConfigurator
{
    /// <inheritdoc />
    public void Configure(
        ISiloBuilder siloBuilder
    )
    {
        // Configure memory streams for SignalR backplane
        siloBuilder.AddMemoryStreams(MississippiDefaults.StreamProviderName);

        // Configure memory grain storage for grain state
        siloBuilder.AddMemoryGrainStorage("signalr-grains");
        siloBuilder.AddMemoryGrainStorage("PubSubStore");
    }
}