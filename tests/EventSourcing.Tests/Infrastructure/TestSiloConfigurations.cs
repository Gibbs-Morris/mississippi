using Mississippi.EventSourcing.Abstractions.Storage;

using Orleans.TestingHost;


namespace Mississippi.EventSourcing.Tests.Infrastructure;

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
                services.AddEventSourcing();
                services.AddSingleton<InMemoryBrookStorage>();
                services.AddSingleton<IBrookStorageReader>(sp => sp.GetRequiredService<InMemoryBrookStorage>());
                services.AddSingleton<IBrookStorageWriter>(sp => sp.GetRequiredService<InMemoryBrookStorage>());
            });

        // Required for memory streams pub/sub validation
        siloBuilder.AddMemoryGrainStorage("PubSubStore");
    }
}