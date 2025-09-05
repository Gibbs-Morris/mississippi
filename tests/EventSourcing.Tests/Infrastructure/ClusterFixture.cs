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
    /// <summary>
    ///     Initializes a new instance of the <see cref="ClusterFixture" /> class and deploys a single-node Orleans
    ///     TestCluster.
    /// </summary>
    public ClusterFixture()
    {
        TestClusterBuilder builder = new();
        builder.AddSiloBuilderConfigurator<TestSiloConfigurations>();
        builder.AddClientBuilderConfigurator<TestClientConfigurations>();
        builder.Options.InitialSilosCount = 1;
        Cluster = builder.Build();
        Cluster.Deploy();
    }

    /// <summary>
    ///     Gets the active Orleans test cluster.
    /// </summary>
    public TestCluster Cluster { get; }

    /// <inheritdoc />
    public void Dispose() => Cluster.StopAllSilos();
}

/// <summary>
///     Client configuration for the Orleans test cluster.
/// </summary>
internal sealed class TestClientConfigurations : IClientBuilderConfigurator
{
    /// <inheritdoc />
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
    /// <summary>
    ///     The xUnit collection name for tests sharing a single Orleans cluster.
    /// </summary>
    public const string Name = nameof(ClusterCollection);
}

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