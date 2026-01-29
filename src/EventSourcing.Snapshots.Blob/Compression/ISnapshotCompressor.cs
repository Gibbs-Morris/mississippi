using System;
using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.EventSourcing.Snapshots.Blob.Compression;

/// <summary>
///     Provides compression and decompression operations for snapshot data.
/// </summary>
internal interface ISnapshotCompressor
{
    /// <summary>
    ///     Gets the content encoding identifier for this compressor.
    /// </summary>
    /// <remarks>
    ///     Standard values: <c>""</c> (none), <c>"gzip"</c>, <c>"br"</c> (Brotli).
    /// </remarks>
    string ContentEncoding { get; }

    /// <summary>
    ///     Compresses the input data.
    /// </summary>
    /// <param name="data">The data to compress.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The compressed data.</returns>
    Task<byte[]> CompressAsync(
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Decompresses the input data.
    /// </summary>
    /// <param name="data">The compressed data.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The decompressed data.</returns>
    Task<byte[]> DecompressAsync(
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default
    );
}