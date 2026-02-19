using System.Threading.Tasks;

using Mississippi.EventSourcing.Snapshots.Blob.Compression;


namespace Mississippi.EventSourcing.Snapshots.Blob.L0Tests.Compression;

/// <summary>
///     Tests for <see cref="BrotliSnapshotCompressor" />.
/// </summary>
public sealed class BrotliSnapshotCompressorTests
{
    /// <summary>
    ///     Verifies that compress and decompress round-trip correctly.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task CompressDecompressShouldRoundTrip()
    {
        BrotliSnapshotCompressor compressor = new();
        byte[] original = "Hello, World! This is test data for Brotli compression."u8.ToArray();
        byte[] compressed = await compressor.CompressAsync(original);
        byte[] decompressed = await compressor.DecompressAsync(compressed);
        Assert.Equal(original, decompressed);
    }

    /// <summary>
    ///     Verifies that compression reduces size for compressible data.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task CompressShouldReduceSizeForCompressibleData()
    {
        BrotliSnapshotCompressor compressor = new();

        // Create highly compressible data (repeated pattern)
        byte[] original = new byte[1000];
        for (int i = 0; i < original.Length; i++)
        {
            original[i] = (byte)(i % 4);
        }

        byte[] compressed = await compressor.CompressAsync(original);
        Assert.True(
            compressed.Length < original.Length,
            $"Compressed size {compressed.Length} should be less than original {original.Length}");
    }

    /// <summary>
    ///     Verifies that ContentEncoding is "br".
    /// </summary>
    [Fact]
    public void ContentEncodingShouldBeBr()
    {
        BrotliSnapshotCompressor compressor = new();
        Assert.Equal("br", compressor.ContentEncoding);
    }

    /// <summary>
    ///     Verifies that empty array round-trips correctly.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task EmptyArrayShouldRoundTrip()
    {
        BrotliSnapshotCompressor compressor = new();
        byte[] original = [];
        byte[] compressed = await compressor.CompressAsync(original);
        byte[] decompressed = await compressor.DecompressAsync(compressed);
        Assert.Empty(decompressed);
    }
}