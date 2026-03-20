#pragma warning disable CS0618 // Testing legacy composition APIs pending issue #237.
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.Aqueduct.Runtime;
using Mississippi.Brooks.Abstractions.Streaming;
using Mississippi.Brooks.Runtime;
using Mississippi.Brooks.Runtime.Storage.Abstractions;
using Mississippi.Inlet.Runtime.Abstractions;
using Mississippi.Testing.Utilities.Storage;

using Orleans.Hosting;
using Orleans.TestingHost;


namespace Mississippi.Inlet.Runtime.L0Tests.Infrastructure;

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
        // Host configures stream infrastructure
        siloBuilder.AddMemoryStreams(BrookStreamingDefaults.OrleansStreamProviderName);
        siloBuilder.AddMemoryGrainStorage("PubSubStore");

        // Tell Brooks which stream provider to use
        siloBuilder.UseEventSourcing();

        // Configure Aqueduct for IAqueductGrainFactory
        siloBuilder.UseAqueduct();
        siloBuilder.ConfigureServices(services =>
        {
            // Register InletSilo services (IProjectionBrookRegistry)
            services.AddInletSilo();

            // Add EventSourcing services for IStreamIdFactory
            services.UseEventSourcingServices();

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

        // Required for memory streams pub/sub validation
        siloBuilder.AddMemoryGrainStorage("PubSubStore");
    }
}

#pragma warning restore CS0618