namespace MississippiTests.DomainModeling.ReplicaSinks.Runtime.L0Tests;

/// <summary>
///     Test contract payload used by replica-sink latest-state processor tests.
/// </summary>
internal sealed class TestContract
{
    /// <summary>
    ///     Gets the test identifier.
    /// </summary>
    public string Id { get; init; } = string.Empty;
}