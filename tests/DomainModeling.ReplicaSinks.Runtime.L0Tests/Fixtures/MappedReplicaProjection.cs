using Mississippi.DomainModeling.ReplicaSinks.Abstractions;


namespace MississippiTests.DomainModeling.ReplicaSinks.Runtime.L0Tests.Fixtures;

/// <summary>
///     A mapped projection used by runtime discovery tests.
/// </summary>
[ProjectionReplication("bootstrap-mapped", "orders-read", typeof(MappedReplicaContract))]
internal sealed class MappedReplicaProjection
{
    /// <summary>
    ///     Gets or sets the sample projection identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;
}