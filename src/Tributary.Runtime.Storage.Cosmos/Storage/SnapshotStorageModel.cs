using System;

using Mississippi.EventSourcing.Snapshots.Abstractions;


namespace Mississippi.EventSourcing.Snapshots.Cosmos.Storage;

/// <summary>
///     Represents a snapshot in storage form prior to Cosmos document serialization.
/// </summary>
internal sealed record SnapshotStorageModel
{
    /// <summary>
    ///     Gets the serialized snapshot payload.
    /// </summary>
    public byte[] Data { get; init; } = Array.Empty<byte>();

    /// <summary>
    ///     Gets the MIME type for the payload.
    /// </summary>
    public string DataContentType { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the size of the snapshot payload in bytes.
    /// </summary>
    public long DataSizeBytes { get; init; }

    /// <summary>
    ///     Gets the stream key for the snapshot.
    /// </summary>
    public SnapshotStreamKey StreamKey { get; init; }

    /// <summary>
    ///     Gets the snapshot version.
    /// </summary>
    public long Version { get; init; }
}