using Orleans.TestingHost;


namespace Mississippi.Testing.Utilities.Orleans;

/// <summary>
///     Provides shared access to the Orleans <see cref="TestCluster" /> for test classes
///     that participate in cluster-based test collections.
/// </summary>
/// <remarks>
///     This static accessor is set by cluster fixture implementations during initialization
///     and provides a convenient way for test classes to access the shared cluster instance.
/// </remarks>
public static class TestClusterAccess
{
    /// <summary>
    ///     Gets or sets the active shared <see cref="TestCluster" /> instance.
    /// </summary>
    /// <remarks>
    ///     This property is set by the cluster fixture during test collection initialization.
    ///     Test classes should access this property only within tests that are part of a
    ///     cluster test collection.
    /// </remarks>
    public static TestCluster Cluster { get; set; } = null!;
}