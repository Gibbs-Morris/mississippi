namespace Mississippi.EventSourcing.Snapshots.Abstractions;

/// <summary>
///     Combines snapshot read, write, delete, and pruning operations for snapshot envelopes.
/// </summary>
public interface ISnapshotStorageProvider
    : ISnapshotStorageReader,
      ISnapshotStorageWriter
{
    /// <summary>
    ///     Gets the storage format identifier for this snapshot provider.
    /// </summary>
    string Format { get; }
}