namespace Mississippi.Tributary.Runtime.Storage.Blob;

/// <summary>
///     Controls payload compression behavior for Blob-backed snapshot storage.
/// </summary>
internal enum SnapshotBlobCompression
{
    /// <summary>
    ///     Stores payload bytes without compression.
    /// </summary>
    Off = 0,

    /// <summary>
    ///     Stores payload bytes using gzip compression.
    /// </summary>
    Gzip = 1,
}