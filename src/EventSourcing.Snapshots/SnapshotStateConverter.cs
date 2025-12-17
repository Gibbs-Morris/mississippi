using System;

using Mississippi.EventSourcing.Serialization.Abstractions;
using Mississippi.EventSourcing.Snapshots.Abstractions;


namespace Mississippi.EventSourcing.Snapshots;

/// <summary>
///     Default implementation of <see cref="ISnapshotStateConverter{TState}" /> that uses
///     <see cref="ISerializationProvider" /> for payload encoding.
/// </summary>
/// <typeparam name="TState">The type of state to convert.</typeparam>
internal sealed class SnapshotStateConverter<TState> : ISnapshotStateConverter<TState>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SnapshotStateConverter{TState}" /> class.
    /// </summary>
    /// <param name="serializationProvider">The provider for state serialization and deserialization.</param>
    public SnapshotStateConverter(
        ISerializationProvider serializationProvider
    ) =>
        SerializationProvider = serializationProvider ?? throw new ArgumentNullException(nameof(serializationProvider));

    /// <summary>
    ///     Gets the serialization provider for state encoding and decoding.
    /// </summary>
    private ISerializationProvider SerializationProvider { get; }

    /// <inheritdoc />
    public TState FromEnvelope(
        SnapshotEnvelope envelope
    )
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ReadOnlyMemory<byte> data = envelope.Data.AsMemory();
        return SerializationProvider.Deserialize<TState>(data);
    }

    /// <inheritdoc />
    public SnapshotEnvelope ToEnvelope(
        TState state,
        string reducerHash
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(reducerHash);
        ReadOnlyMemory<byte> data = SerializationProvider.Serialize(state);
        return new()
        {
            Data = [.. data.Span],
            DataContentType = SerializationProvider.Format,
            ReducerHash = reducerHash,
        };
    }
}