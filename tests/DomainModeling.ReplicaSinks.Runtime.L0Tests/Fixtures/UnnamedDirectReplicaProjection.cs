namespace MississippiTests.DomainModeling.ReplicaSinks.Runtime.L0Tests.Fixtures;

/// <summary>
///     A direct-replication projection without stable contract metadata used by validation tests.
/// </summary>
internal sealed class UnnamedDirectReplicaProjection
{
    /// <summary>
    ///     Gets or sets the sample projection identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;
}