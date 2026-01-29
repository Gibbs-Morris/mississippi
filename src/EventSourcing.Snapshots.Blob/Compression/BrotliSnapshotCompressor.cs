using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.EventSourcing.Snapshots.Blob.Compression;

/// <summary>
///     Brotli compression implementation.
/// </summary>
/// <remarks>
///     Brotli offers better compression ratios than GZip and faster decompression,
///     making it ideal for snapshot storage where reads are more frequent than writes.
/// </remarks>
internal sealed class BrotliSnapshotCompressor : ISnapshotCompressor
{
    /// <inheritdoc />
    public string ContentEncoding => "br";

    /// <inheritdoc />
    public async Task<byte[]> CompressAsync(
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default
    )
    {
        using MemoryStream output = new();
        await using (BrotliStream brotli = new(output, CompressionLevel.Optimal, true))
        {
            await brotli.WriteAsync(data, cancellationToken).ConfigureAwait(false);
        }

        return output.ToArray();
    }

    /// <inheritdoc />
    public async Task<byte[]> DecompressAsync(
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default
    )
    {
        using MemoryStream input = new(data.ToArray());
        await using BrotliStream brotli = new(input, CompressionMode.Decompress);
        using MemoryStream output = new();
        await brotli.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
        return output.ToArray();
    }
}