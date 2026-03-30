using Mississippi.DomainModeling.ReplicaSinks.Abstractions;


namespace MississippiTests.DomainModeling.ReplicaSinks.Abstractions.L0Tests;

/// <summary>
///     A sample direct-replication projection used by abstraction tests.
/// </summary>
[ReplicaContractName("TestApp", "Orders", "DirectReplica")]
[ProjectionReplication("search", "orders-direct", IsDirectProjectionReplicationEnabled = true)]
internal sealed class SampleDirectReplicaProjection
{
    /// <summary>
    ///     Gets or sets a sample value.
    /// </summary>
    public string Value { get; set; } = string.Empty;
}