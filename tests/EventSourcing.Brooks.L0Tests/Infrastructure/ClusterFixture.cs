using System;

using Orleans.TestingHost;


namespace Mississippi.EventSourcing.Tests.Infrastructure;

/// <summary>
///     Shared Orleans TestCluster fixture for EventSourcing grain tests.
/// </summary>
internal sealed class ClusterFixture : IDisposable
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
        TestClusterAccess.Cluster = Cluster;
    }

    /// <summary>
    ///     Gets the active Orleans test cluster.
    /// </summary>
    public TestCluster Cluster { get; }

    /// <inheritdoc />
    public void Dispose() => Cluster.StopAllSilos();
}