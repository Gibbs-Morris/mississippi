using Mississippi.DomainModeling.ReplicaSinks.Abstractions;


namespace MississippiTests.DomainModeling.ReplicaSinks.Runtime.L0Tests.Fixtures;

/// <summary>
///     A direct-replication projection used by runtime discovery tests.
/// </summary>
[ProjectionReplication("bootstrap-direct", "orders-direct", IsDirectProjectionReplicationEnabled = true)]
internal sealed class DirectReplicaProjection
{
    /// <summary>
    ///     Gets or sets the sample projection identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;
}