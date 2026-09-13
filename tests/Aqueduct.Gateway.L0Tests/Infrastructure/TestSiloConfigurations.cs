using Mississippi.Aqueduct.Runtime;
using Mississippi.Hosting.Runtime;

using Orleans.Hosting;
using Orleans.TestingHost;


namespace Mississippi.Aqueduct.Gateway.L0Tests.Infrastructure;

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
        // Configure the SignalR backplane through the canonical runtime composition path.
        siloBuilder.UseMississippi(runtime => runtime.AddAqueduct(aqueduct => aqueduct.UseMemoryStreams()));

        // Configure memory grain storage for grain state
        siloBuilder.AddMemoryGrainStorage("signalr-grains");
    }
}