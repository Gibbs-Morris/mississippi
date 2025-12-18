using Orleans.TestingHost;


namespace Mississippi.EventSourcing.UxProjections.L0Tests.Infrastructure;

/// <summary>
///     Provides access to the shared Orleans <see cref="TestCluster" /> for test classes that participate in the
///     <see cref="ClusterTestSuite" /> collection.
/// </summary>
internal static class TestClusterAccess
{
    /// <summary>
    ///     Gets or sets the active shared <see cref="TestCluster" /> instance initialized by <see cref="ClusterFixture" />.
    /// </summary>
    public static TestCluster Cluster { get; internal set; } = null!;
}