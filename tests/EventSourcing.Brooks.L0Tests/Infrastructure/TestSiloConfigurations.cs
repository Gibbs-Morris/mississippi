using Microsoft.Extensions.DependencyInjection;

using Mississippi.EventSourcing.Brooks.Abstractions.Storage;
using Mississippi.Testing.Utilities.Storage;

using Orleans.Hosting;
using Orleans.TestingHost;


namespace Mississippi.EventSourcing.Brooks.L0Tests.Infrastructure;

/// <summary>
///     Silo configuration for the EventSourcing test cluster.
/// </summary>
internal sealed class TestSiloConfigurations : ISiloConfigurator
{
    /// <inheritdoc />
    public void Configure(
        ISiloBuilder siloBuilder
    )
    {
        siloBuilder.AddEventSourcing()
            .ConfigureServices(services =>
            {
                services.AddEventSourcingByService();
                services.AddSingleton<InMemoryBrookStorage>();
                services.AddSingleton<IBrookStorageReader>(sp => sp.GetRequiredService<InMemoryBrookStorage>());
                services.AddSingleton<IBrookStorageWriter>(sp => sp.GetRequiredService<InMemoryBrookStorage>());
            });

        // Required for memory streams pub/sub validation
        siloBuilder.AddMemoryGrainStorage("PubSubStore");
    }
}