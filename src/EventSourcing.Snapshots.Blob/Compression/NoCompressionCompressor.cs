using System;
using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.EventSourcing.Snapshots.Blob.Compression;

/// <summary>
///     A no-op compressor that returns data unchanged.
/// </summary>
internal sealed class NoCompressionCompressor : ISnapshotCompressor
{
    /// <inheritdoc />
    public string ContentEncoding => string.Empty;

    /// <inheritdoc />
    public Task<byte[]> CompressAsync(
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default
    ) =>
        Task.FromResult(data.ToArray());

    /// <inheritdoc />
    public Task<byte[]> DecompressAsync(
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default
    ) =>
        Task.FromResult(data.ToArray());
}