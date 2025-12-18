using System.Collections.Immutable;

using Orleans;


namespace Mississippi.EventSourcing.Snapshots.Abstractions;

/// <summary>
///     Represents a serialized snapshot payload and its content type.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.Snapshots.Abstractions.SnapshotEnvelope")]
public sealed record SnapshotEnvelope
{
    /// <summary>
    ///     Gets the raw snapshot payload.
    /// </summary>
    [Id(0)]
    public ImmutableArray<byte> Data { get; init; } = ImmutableArray<byte>.Empty;

    /// <summary>
    ///     Gets the MIME type that describes the <see cref="Data" /> payload.
    /// </summary>
    [Id(1)]
    public string DataContentType { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the hash of the reducers used to create this snapshot.
    ///     Used for invalidation when reducer logic changes.
    /// </summary>
    /// <remarks>
    ///     When this value is empty or does not match the current reducer hash,
    ///     the snapshot is considered stale and must be rebuilt from the event stream.
    /// </remarks>
    [Id(2)]
    public string ReducerHash { get; init; } = string.Empty;
}