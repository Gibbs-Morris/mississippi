using System;
using System.IO;
using System.IO.Compression;


namespace Mississippi.Tributary.Runtime.Storage.Blobs.Storage;

/// <summary>
///     Compresses and decompresses Blob snapshot payload bytes.
/// </summary>
internal static class SnapshotBlobCompression
{
    /// <summary>
    ///     The metadata value indicating a gzip-compressed payload.
    /// </summary>
    public const string Gzip = "gzip";

    /// <summary>
    ///     The metadata value indicating an uncompressed payload.
    /// </summary>
    public const string None = "none";

    private const int BufferSize = 81920;

    /// <summary>
    ///     Compresses the payload when compression is enabled.
    /// </summary>
    /// <param name="payload">The payload bytes.</param>
    /// <param name="enableCompression">A value indicating whether gzip compression should be applied.</param>
    /// <returns>The stored bytes and compression metadata.</returns>
    public static SnapshotBlobCompressionResult Compress(
        byte[] payload,
        bool enableCompression
    )
    {
        ArgumentNullException.ThrowIfNull(payload);
        if (!enableCompression)
        {
            return new(None, payload);
        }

        using MemoryStream output = new();
        using (GZipStream gzipStream = new(output, CompressionLevel.SmallestSize, true))
        {
            gzipStream.Write(payload, 0, payload.Length);
        }

        return new(Gzip, output.ToArray());
    }

    /// <summary>
    ///     Decompresses stored payload bytes according to the stored compression metadata.
    /// </summary>
    /// <param name="compression">The stored compression value.</param>
    /// <param name="storedBytes">The stored payload bytes before Base64 encoding.</param>
    /// <param name="maximumPayloadSizeBytes">The maximum allowed uncompressed payload size in bytes.</param>
    /// <returns>The uncompressed payload bytes.</returns>
    public static byte[] Decompress(
        string compression,
        byte[] storedBytes,
        long maximumPayloadSizeBytes = long.MaxValue
    )
    {
        ArgumentNullException.ThrowIfNull(compression);
        ArgumentNullException.ThrowIfNull(storedBytes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumPayloadSizeBytes);
        if (string.Equals(compression, None, StringComparison.Ordinal))
        {
            if (storedBytes.LongLength > maximumPayloadSizeBytes)
            {
                throw new InvalidDataException(
                    $"Uncompressed snapshot payload size '{storedBytes.LongLength}' exceeds the configured maximum '{maximumPayloadSizeBytes}'.");
            }

            return storedBytes;
        }

        if (!string.Equals(compression, Gzip, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Unsupported snapshot compression value '{compression}'.");
        }

        try
        {
            using MemoryStream input = new(storedBytes, false);
            using GZipStream gzipStream = new(input, CompressionMode.Decompress);
            using MemoryStream output = new();
            byte[] buffer = new byte[BufferSize];
            long totalBytesRead = 0;
            while (true)
            {
                int bytesRead = gzipStream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    break;
                }

                totalBytesRead += bytesRead;
                if (totalBytesRead > maximumPayloadSizeBytes)
                {
                    throw new InvalidDataException(
                        $"Decompressed snapshot payload size exceeds the configured maximum '{maximumPayloadSizeBytes}'.");
                }

                output.Write(buffer, 0, bytesRead);
            }

            return output.ToArray();
        }
        catch (InvalidDataException exception)
        {
            throw new InvalidDataException("Stored gzip snapshot payload is invalid.", exception);
        }
    }
}