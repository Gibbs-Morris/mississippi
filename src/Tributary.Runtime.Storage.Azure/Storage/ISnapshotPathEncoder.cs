using Mississippi.Tributary.Abstractions;


namespace Mississippi.Tributary.Runtime.Storage.Azure.Storage
{
    /// <summary>
    ///     Encodes Tributary Azure blob paths using deterministic snapshot stream prefixes.
    /// </summary>
    internal interface ISnapshotPathEncoder
    {
        /// <summary>
        ///     Gets the fully qualified blob path for a specific snapshot version.
        /// </summary>
        /// <param name="snapshotKey">The snapshot key.</param>
        /// <returns>The snapshot blob path.</returns>
        string GetSnapshotPath(SnapshotKey snapshotKey);

        /// <summary>
        ///     Gets the deterministic stream prefix used for prefix-based delete and prune operations.
        /// </summary>
        /// <param name="streamKey">The snapshot stream key.</param>
        /// <returns>The stream prefix.</returns>
        string GetStreamPrefix(SnapshotStreamKey streamKey);
    }
}
