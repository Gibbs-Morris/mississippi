using System;


namespace Mississippi.EventSourcing.Snapshots.Blob;

/// <summary>
///     Detects compression in byte arrays by examining magic bytes.
/// </summary>
internal static class CompressionDetector
{
    /// <summary>
    ///     Detects if the data appears to be brotli-compressed.
    /// </summary>
    /// <param name="data">The data to examine.</param>
    /// <returns><c>true</c> if the data appears to use brotli compression; otherwise, <c>false</c>.</returns>
    /// <remarks>
    ///     <para>
    ///         Brotli does not have universal magic bytes like gzip, but typical brotli streams
    ///         start with specific window bits patterns. This is a best-effort heuristic.
    ///     </para>
    /// </remarks>
    public static bool IsBrotli(
        ReadOnlySpan<byte> data
    )
    {
        // Brotli detection is approximate; check for common patterns
        // A typical brotli stream window size indicator in first byte
        if (data.Length < 1)
        {
            return false;
        }

        // Brotli first byte encodes WBITS; common values are in specific ranges
        // This is heuristic and may have false positives/negatives
        byte firstByte = data[0];
        return ((firstByte & 0x0F) >= 0x0A) && ((firstByte & 0x0F) <= 0x0F);
    }

    /// <summary>
    ///     Detects if the data appears to use any known compression format.
    /// </summary>
    /// <param name="data">The data to examine.</param>
    /// <returns><c>true</c> if compression is detected; otherwise, <c>false</c>.</returns>
    public static bool IsCompressed(
        ReadOnlySpan<byte> data
    ) =>
        IsGzip(data) || IsBrotli(data);

    /// <summary>
    ///     Detects if the data appears to be gzip-compressed.
    /// </summary>
    /// <param name="data">The data to examine.</param>
    /// <returns><c>true</c> if the data starts with gzip magic bytes (0x1F 0x8B); otherwise, <c>false</c>.</returns>
    public static bool IsGzip(
        ReadOnlySpan<byte> data
    )
    {
        if (data.Length < 2)
        {
            return false;
        }

        return (data[0] == 0x1F) && (data[1] == 0x8B);
    }
}