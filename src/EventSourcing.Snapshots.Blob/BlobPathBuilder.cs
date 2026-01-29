using System.Globalization;

using Mississippi.EventSourcing.Snapshots.Abstractions;


namespace Mississippi.EventSourcing.Snapshots.Blob;

/// <summary>
///     Builds Azure Blob Storage paths for snapshot persistence.
/// </summary>
internal static class BlobPathBuilder
{
    /// <summary>
    ///     Builds a blob path from snapshot key components using the recommended structure.
    /// </summary>
    /// <param name="snapshotKey">The snapshot key containing storage name, entity ID, reducers hash, and version.</param>
    /// <returns>A blob path in the format: {storageName}/{entityId}/{reducersHash}/{version}.</returns>
    /// <remarks>
    ///     <para>
    ///         The path structure ensures:
    ///         - Efficient blob prefix queries by storage name and entity ID
    ///         - Natural grouping of versions for the same projection
    ///         - Reducers hash isolation for when reducer logic changes
    ///     </para>
    /// </remarks>
    public static string BuildPath(
        SnapshotKey snapshotKey
    )
    {
        string storageName = snapshotKey.Stream.SnapshotStorageName;
        string entityId = snapshotKey.Stream.EntityId;
        string reducersHash = snapshotKey.Stream.ReducersHash;
        string version = snapshotKey.Version.ToString(CultureInfo.InvariantCulture);
        return $"{storageName}/{entityId}/{reducersHash}/{version}";
    }

    /// <summary>
    ///     Builds a blob path prefix for listing all snapshots in a stream.
    /// </summary>
    /// <param name="streamKey">The stream key containing storage name, entity ID, and reducers hash.</param>
    /// <returns>A blob path prefix in the format: {storageName}/{entityId}/{reducersHash}/</returns>
    public static string BuildStreamPrefix(
        SnapshotStreamKey streamKey
    )
    {
        string storageName = streamKey.SnapshotStorageName;
        string entityId = streamKey.EntityId;
        string reducersHash = streamKey.ReducersHash;
        return $"{storageName}/{entityId}/{reducersHash}/";
    }
}