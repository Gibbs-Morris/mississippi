using System;
using System.IO;
using System.IO.Compression;


namespace Mississippi.Tributary.Runtime.Storage.Blob;

/// <summary>
///     Provides gzip compression helpers for snapshot payload storage.
/// </summary>
internal static class SnapshotCompression
{
    /// <summary>
    ///     Compresses snapshot payload bytes using gzip.
    /// </summary>
    /// <param name="data">The raw snapshot payload.</param>
    /// <returns>The compressed payload.</returns>
    public static byte[] Compress(
        byte[] data
    )
    {
        ArgumentNullException.ThrowIfNull(data);
        using MemoryStream output = new();
        using (GZipStream gzipStream = new(output, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            gzipStream.Write(data, 0, data.Length);
        }

        return output.ToArray();
    }

    /// <summary>
    ///     Decompresses gzip-compressed snapshot payload bytes.
    /// </summary>
    /// <param name="data">The compressed snapshot payload.</param>
    /// <returns>The decompressed payload.</returns>
    public static byte[] Decompress(
        byte[] data
    )
    {
        ArgumentNullException.ThrowIfNull(data);
        using MemoryStream input = new(data);
        using GZipStream gzipStream = new(input, CompressionMode.Decompress);
        using MemoryStream output = new();
        gzipStream.CopyTo(output);
        return output.ToArray();
    }
}
