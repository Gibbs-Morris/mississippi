using Mississippi.Hosting.Runtime;

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
        siloBuilder.UseMississippi(runtime => runtime.AddAqueduct(aqueduct => aqueduct.UseMemoryStreams()));

        // Configure memory grain storage for grain state
        siloBuilder.AddMemoryGrainStorage("signalr-grains");
    }
}