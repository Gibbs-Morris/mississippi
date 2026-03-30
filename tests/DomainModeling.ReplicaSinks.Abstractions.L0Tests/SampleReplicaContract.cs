namespace MississippiTests.DomainModeling.ReplicaSinks.Abstractions.L0Tests;

/// <summary>
///     A sample replica contract used by abstraction tests.
/// </summary>
internal sealed class SampleReplicaContract
{
    /// <summary>
    ///     Gets or sets a sample value.
    /// </summary>
    public string Value { get; set; } = string.Empty;
}