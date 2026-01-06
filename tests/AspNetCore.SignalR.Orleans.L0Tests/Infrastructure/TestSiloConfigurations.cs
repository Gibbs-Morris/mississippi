using Microsoft.Extensions.DependencyInjection;

using Orleans.Hosting;
using Orleans.TestingHost;


namespace Mississippi.AspNetCore.SignalR.Orleans.L0Tests.Infrastructure;

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
        siloBuilder.AddMemoryStreams("SignalRStreams");

        // Configure memory grain storage for grain state
        siloBuilder.AddMemoryGrainStorage("signalr-grains");
        siloBuilder.AddMemoryGrainStorage("PubSubStore");

        // Configure SignalR options
        siloBuilder.ConfigureServices(services =>
        {
            services.Configure<OrleansSignalROptions>(options =>
            {
                options.StreamProviderName = "SignalRStreams";
            });
        });
    }
}