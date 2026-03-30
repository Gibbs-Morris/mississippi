namespace MississippiTests.DomainModeling.ReplicaSinks.Runtime.L0Tests.Fixtures;

/// <summary>
///     A contract fixture that intentionally omits replica contract naming metadata.
/// </summary>
internal sealed class UnnamedReplicaContract
{
    /// <summary>
    ///     Gets or sets the sample contract identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;
}