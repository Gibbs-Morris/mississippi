using Mississippi.DomainModeling.ReplicaSinks.Abstractions;


namespace MississippiTests.DomainModeling.ReplicaSinks.Runtime.L0Tests.Fixtures;

/// <summary>
///     A mapped replica contract used by runtime discovery tests.
/// </summary>
[ReplicaContractName("TestApp", "Orders", "MappedReplica")]
internal sealed class MappedReplicaContract
{
    /// <summary>
    ///     Gets or sets the sample contract identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;
}