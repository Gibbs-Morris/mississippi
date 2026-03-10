using System.Linq;

using Mississippi.Tributary.Runtime.Storage.Blob;


namespace Mississippi.Tributary.Runtime.Storage.Blob.L0Tests;

/// <summary>
///     Tests for snapshot compression helpers.
/// </summary>
public sealed class SnapshotCompressionTests
{
    /// <summary>
    ///     Ensures compressed content can be decompressed back to the original bytes.
    /// </summary>
    [Fact]
    public void CompressAndDecompressShouldRoundTrip()
    {
        byte[] data = Enumerable.Range(0, 512).Select(value => (byte)(value % 256)).ToArray();
        byte[] compressed = SnapshotCompression.Compress(data);
        byte[] decompressed = SnapshotCompression.Decompress(compressed);
        Assert.Equal(data, decompressed);
    }
}
