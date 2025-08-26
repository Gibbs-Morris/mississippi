using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Mississippi.EventSourcing.Abstractions.Storage;

using Orleans.TestingHost;


namespace Mississippi.EventSourcing.Tests.Infrastructure;

/// <summary>
///     Shared Orleans TestCluster fixture for EventSourcing grain tests.
/// </summary>
public sealed class ClusterFixture : IDisposable
{
    public ClusterFixture()
    {
        TestClusterBuilder builder = new();
        builder.AddSiloBuilderConfigurator<TestSiloConfigurations>();
        builder.AddClientBuilderConfigurator<TestClientConfigurations>();
        builder.Options.InitialSilosCount = 1;
        Cluster = builder.Build();
        Cluster.Deploy();
    }

    public TestCluster Cluster { get; }

    public void Dispose() => Cluster.StopAllSilos();
}

internal sealed class TestClientConfigurations : IClientBuilderConfigurator
{
    public void Configure(
        IConfiguration configuration,
        IClientBuilder clientBuilder
    )
    {
        clientBuilder.AddMemoryStreams("MississippiBrookStreamProvider");
    }
}

/// <summary>
///     xUnit collection definition to share a single TestCluster across tests.
/// </summary>
[CollectionDefinition(Name)]
public sealed class ClusterCollection : ICollectionFixture<ClusterFixture>
{
    public const string Name = nameof(ClusterCollection);
}

/// <summary>
///     Silo configuration for the EventSourcing test cluster.
/// </summary>
internal sealed class TestSiloConfigurations : ISiloConfigurator
{
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