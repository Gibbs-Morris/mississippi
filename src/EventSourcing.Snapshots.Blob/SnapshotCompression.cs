namespace Mississippi.EventSourcing.Snapshots.Blob;

/// <summary>
///     Specifies the compression algorithm to use when writing snapshots.
/// </summary>
public enum SnapshotCompression
{
    /// <summary>
    ///     No compression is applied.
    /// </summary>
    None = 0,

    /// <summary>
    ///     GZip compression (good compatibility).
    /// </summary>
    GZip = 1,

    /// <summary>
    ///     Brotli compression (better compression ratio, faster decompression).
    /// </summary>
    Brotli = 2,
}