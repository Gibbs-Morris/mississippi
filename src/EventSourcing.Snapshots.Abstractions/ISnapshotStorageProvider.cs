namespace Mississippi.EventSourcing.Snapshots.Abstractions;

/// <summary>
///     Combines snapshot read, write, delete, and pruning operations for a given projection snapshot type.
/// </summary>
/// <typeparam name="TProjection">The projection snapshot model.</typeparam>
public interface ISnapshotStorageProvider<TProjection>
    : ISnapshotStorageReader<TProjection>,
      ISnapshotStorageWriter<TProjection>
{
    /// <summary>
    ///     Gets the storage format identifier for this snapshot provider.
    /// </summary>
    string Format { get; }
}