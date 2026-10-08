using Mississippi.Tributary.Abstractions;
using Mississippi.Tributary.Runtime.L0Tests;


namespace MississippiTests.Tributary.Runtime.L0Tests;

/// <summary>
///     Handles a different event type for independent reducer order tests.
/// </summary>
internal sealed class TextStateEventReducer : EventReducerBase<string, SnapshotCacheGrainTestState>
{
    /// <inheritdoc />
    protected override SnapshotCacheGrainTestState ReduceCore(
        SnapshotCacheGrainTestState state,
        string eventData
    ) =>
        state with
        {
            Value = state.Value + eventData.Length,
        };
}