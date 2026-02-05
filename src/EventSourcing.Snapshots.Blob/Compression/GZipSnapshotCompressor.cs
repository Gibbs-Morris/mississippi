using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.EventSourcing.Snapshots.Blob.Compression;

/// <summary>
///     GZip compression implementation.
/// </summary>
internal sealed class GZipSnapshotCompressor : ISnapshotCompressor
{
    /// <inheritdoc />
    public string ContentEncoding => "gzip";

    /// <inheritdoc />
    public async Task<byte[]> CompressAsync(
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default
    )
    {
        using MemoryStream output = new();
        await using (GZipStream gzip = new(output, CompressionLevel.Optimal, true))
        {
            await gzip.WriteAsync(data, cancellationToken).ConfigureAwait(false);
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
        await using GZipStream gzip = new(input, CompressionMode.Decompress);
        using MemoryStream output = new();
        await gzip.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
        return output.ToArray();
    }
}