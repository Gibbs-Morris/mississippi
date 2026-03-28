using System;
using System.Collections.Immutable;
using System.Linq;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Runtime.Storage.Azure.Storage;


namespace Mississippi.Brooks.Runtime.Storage.Azure.L0Tests;

/// <summary>
///     Tests for <see cref="AzureBrookEventDocumentCodec" />.
/// </summary>
public sealed class AzureBrookEventDocumentCodecTests
{
    /// <summary>
    ///     Encode and decode preserve the Brooks event payload.
    /// </summary>
    [Fact]
    public void EncodeAndDecodeRoundTripPreservesBrookEvent()
    {
        AzureBrookEventDocumentCodec codec = new();
        BrookEvent brookEvent = new()
        {
            Id = "event-1",
            Source = "orders",
            EventType = "OrderCreated",
            DataContentType = "application/json",
            Data = ImmutableArray.Create<byte>(1, 2, 3, 4),
            DataSizeBytes = 4,
            Time = new(2026, 3, 28, 0, 0, 0, TimeSpan.Zero),
        };

        BrookEvent decoded = codec.Decode(codec.Encode(brookEvent, 7));

        Assert.Equal(brookEvent.Id, decoded.Id);
        Assert.Equal(brookEvent.Source, decoded.Source);
        Assert.Equal(brookEvent.EventType, decoded.EventType);
        Assert.Equal(brookEvent.DataContentType, decoded.DataContentType);
        Assert.Equal(brookEvent.DataSizeBytes, decoded.DataSizeBytes);
        Assert.Equal(brookEvent.Time, decoded.Time);
        Assert.Equal(brookEvent.Data.ToArray(), decoded.Data.ToArray());
    }
}