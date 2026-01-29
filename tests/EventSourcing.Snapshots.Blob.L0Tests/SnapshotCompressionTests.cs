using System;
using System.Linq;


namespace Mississippi.EventSourcing.Snapshots.Blob.L0Tests;

/// <summary>
///     Tests for <see cref="SnapshotCompression" />.
/// </summary>
public sealed class SnapshotCompressionTests
{
    /// <summary>
    ///     Verifies all enum values are unique.
    /// </summary>
    [Fact]
    public void AllValuesShouldBeUnique()
    {
        SnapshotCompression[] values = Enum.GetValues<SnapshotCompression>();
        int[] intValues = values.Select(v => (int)v).ToArray();
        Assert.Equal(intValues.Length, intValues.Distinct().Count());
    }

    /// <summary>
    ///     Verifies Brotli has correct value.
    /// </summary>
    [Fact]
    public void BrotliHasValueTwo()
    {
        Assert.Equal(2, (int)SnapshotCompression.Brotli);
    }

    /// <summary>
    ///     Verifies GZip has correct value.
    /// </summary>
    [Fact]
    public void GZipHasValueOne()
    {
        Assert.Equal(1, (int)SnapshotCompression.GZip);
    }

    /// <summary>
    ///     Verifies None has correct value.
    /// </summary>
    [Fact]
    public void NoneHasValueZero()
    {
        Assert.Equal(0, (int)SnapshotCompression.None);
    }
}