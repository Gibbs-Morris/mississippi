using System.Collections.Immutable;

using Mississippi.Tributary.Abstractions;


namespace Mississippi.Tributary.Runtime.Storage.Abstractions.L0Tests;

/// <summary>
///     Tests for snapshot storage abstraction contracts.
/// </summary>
public sealed class SnapshotAbstractionsTests
{
    /// <summary>
    ///     Ensures the envelope defaults are empty and can be initialized.
    /// </summary>
    [Fact]
    public void SnapshotEnvelopeShouldRoundTripData()
    {
        SnapshotEnvelope envelope = new();
        Assert.Equal(ImmutableArray<byte>.Empty, envelope.Data);
        Assert.Equal(string.Empty, envelope.DataContentType);
        ImmutableArray<byte> data = ImmutableArray.Create((byte)1, (byte)2);
        SnapshotEnvelope populated = new()
        {
            Data = data,
            DataContentType = "application/octet-stream",
        };
        Assert.Equal(data, populated.Data);
        Assert.Equal("application/octet-stream", populated.DataContentType);
    }
}