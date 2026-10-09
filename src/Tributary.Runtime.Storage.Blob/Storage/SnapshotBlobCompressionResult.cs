namespace Mississippi.Tributary.Runtime.Storage.Blob.Storage;

/// <summary>
///     Represents the stored payload bytes and associated compression metadata.
/// </summary>
internal readonly record struct SnapshotBlobCompressionResult(string Compression, byte[] StoredBytes)
{
    /// <summary>
    ///     Gets the number of stored bytes before Base64 encoding.
    /// </summary>
    public long StoredSizeBytes => StoredBytes.LongLength;
}