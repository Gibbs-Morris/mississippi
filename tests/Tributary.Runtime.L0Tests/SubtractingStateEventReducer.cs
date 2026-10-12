using Mississippi.Tributary.Abstractions;
using Mississippi.Tributary.Runtime.L0Tests;


namespace MississippiTests.Tributary.Runtime.L0Tests;

/// <summary>
///     Subtracts the event value from an immutable snapshot state for reducer priority tests.
/// </summary>
internal sealed class SubtractingStateEventReducer : EventReducerBase<int, SnapshotCacheGrainTestState>
{
    /// <inheritdoc />
    protected override SnapshotCacheGrainTestState ReduceCore(
        SnapshotCacheGrainTestState state,
        int eventData
    ) =>
        state with
        {
            Value = state.Value - eventData,
        };
}