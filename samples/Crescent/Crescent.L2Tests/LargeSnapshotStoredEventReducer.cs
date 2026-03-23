using Mississippi.Tributary.Abstractions;


namespace MississippiSamples.Crescent.L2Tests;

/// <summary>
///     Reducer for <see cref="LargeSnapshotStored" /> events.
/// </summary>
internal sealed class LargeSnapshotStoredEventReducer : EventReducerBase<LargeSnapshotStored, LargeSnapshotAggregate>
{
    /// <inheritdoc />
    protected override LargeSnapshotAggregate ReduceCore(
        LargeSnapshotAggregate state,
        LargeSnapshotStored @event
    )
    {
        ArgumentNullException.ThrowIfNull(@event);
        return (state ?? new()) with
        {
            Marker = @event.Marker,
            Payload = @event.Payload,
            PayloadLength = @event.Payload.Length,
        };
    }
}