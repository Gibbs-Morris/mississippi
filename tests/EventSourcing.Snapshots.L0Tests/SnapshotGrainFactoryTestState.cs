namespace Mississippi.EventSourcing.Snapshots.L0Tests;

/// <summary>
///     Test state for factory tests. Contains an example value property for state representation.
/// </summary>
/// <param name="Value">Gets an example value for the test state.</param>
internal sealed record SnapshotGrainFactoryTestState(int Value = 0);