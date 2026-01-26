namespace Mississippi.Inlet.Silo.L0Tests.Infrastructure;

/// <summary>
///     xUnit collection definition to share a single TestCluster across tests.
/// </summary>
[CollectionDefinition(Name)]
#pragma warning disable CA1515 // xUnit requires collection definition classes to be public to be discovered.
public sealed class ClusterTestSuite : ICollectionFixture<ClusterFixture>
#pragma warning restore CA1515
{
    /// <summary>
    ///     The xUnit collection name for tests sharing a single Orleans cluster.
    /// </summary>
    public const string Name = nameof(ClusterTestSuite);
}
