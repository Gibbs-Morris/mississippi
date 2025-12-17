namespace Mississippi.EventSourcing.Snapshots.Abstractions;

/// <summary>
///     Converts between typed snapshot state and serialized <see cref="SnapshotEnvelope" /> payloads.
/// </summary>
/// <typeparam name="TSnapshot">The type of state to convert.</typeparam>
public interface ISnapshotStateConverter<TSnapshot>
{
    /// <summary>
    ///     Deserializes a <see cref="SnapshotEnvelope" /> to typed state.
    /// </summary>
    /// <param name="envelope">The envelope containing the serialized state.</param>
    /// <returns>The deserialized state.</returns>
    TSnapshot FromEnvelope(
        SnapshotEnvelope envelope
    );

    /// <summary>
    ///     Serializes state to a <see cref="SnapshotEnvelope" /> with the specified reducer hash.
    /// </summary>
    /// <param name="state">The state to serialize.</param>
    /// <param name="reducerHash">The hash of the reducers used to build this state.</param>
    /// <returns>A <see cref="SnapshotEnvelope" /> containing the serialized state.</returns>
    SnapshotEnvelope ToEnvelope(
        TSnapshot state,
        string reducerHash
    );
}