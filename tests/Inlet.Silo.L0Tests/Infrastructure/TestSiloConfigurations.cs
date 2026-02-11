using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.Aqueduct.Grains;
using Mississippi.Common.Abstractions;
using Mississippi.Common.Abstractions.Builders;
using Mississippi.EventSourcing.Brooks;
using Mississippi.EventSourcing.Brooks.Abstractions.Storage;
using Mississippi.Inlet.Silo.Abstractions;
using Mississippi.Sdk.Silo;
using Mississippi.Testing.Utilities.Storage;

using Orleans.Hosting;
using Orleans.TestingHost;


namespace Mississippi.Inlet.Silo.L0Tests.Infrastructure;

/// <summary>
///     Silo configuration for the Inlet Orleans test cluster.
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
        siloBuilder.AddMemoryGrainStorage("PubSubStore");

        // Tell Brooks which stream provider to use
        mississippi.AddEventSourcing();

        // Register InletSilo services (IProjectionBrookRegistry)
        mississippi.AddInletSilo();

        // Configure Aqueduct for IAqueductGrainFactory
        siloBuilder.UseAqueduct();
        mississippi.ConfigureServices(services =>
        {
            // In-memory brook storage for tests
            services.AddSingleton<InMemoryBrookStorage>();
            services.AddSingleton<IBrookStorageReader>(sp => sp.GetRequiredService<InMemoryBrookStorage>());

            // Replace the registry with one pre-populated with test projections
            services.RemoveAll<IProjectionBrookRegistry>();
            services.AddSingleton<IProjectionBrookRegistry>(_ =>
            {
                ProjectionBrookRegistry registry = new();
                registry.Register(TestProjections.TestProjection, TestProjections.TestBrook);
                registry.Register(TestProjections.TestProjection2, TestProjections.TestBrook2);
                registry.Register(TestProjections.TestProjection3, TestProjections.TestBrook3);
                registry.Register(TestProjections.TestProjection4, TestProjections.TestBrook4);
                return registry;
            });
        });
    }
}