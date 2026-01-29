using System.Threading.Tasks;

using Mississippi.EventSourcing.Snapshots.Blob.Compression;


namespace Mississippi.EventSourcing.Snapshots.Blob.L0Tests.Compression;

/// <summary>
///     Tests for <see cref="NoCompressionCompressor" />.
/// </summary>
public sealed class NoCompressionCompressorTests
{
    /// <summary>
    ///     Verifies that compress returns the input unchanged.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task CompressAsyncShouldReturnInputUnchanged()
    {
        NoCompressionCompressor compressor = new();
        byte[] input = [1, 2, 3, 4, 5];
        byte[] result = await compressor.CompressAsync(input);
        Assert.Equal(input, result);
    }

    /// <summary>
    ///     Verifies that ContentEncoding is empty for no compression.
    /// </summary>
    [Fact]
    public void ContentEncodingShouldBeEmpty()
    {
        NoCompressionCompressor compressor = new();
        Assert.Empty(compressor.ContentEncoding);
    }

    /// <summary>
    ///     Verifies that decompress returns the input unchanged.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task DecompressAsyncShouldReturnInputUnchanged()
    {
        NoCompressionCompressor compressor = new();
        byte[] input = [1, 2, 3, 4, 5];
        byte[] result = await compressor.DecompressAsync(input);
        Assert.Equal(input, result);
    }
}