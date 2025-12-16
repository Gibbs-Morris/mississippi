using System;
using System.Collections.Immutable;

using Allure.Xunit.Attributes;

using Mississippi.Core.Projection;


namespace Mississippi.Core.L0Tests;

/// <summary>
///     Tests for <see cref="MississippiProjection" />.
/// </summary>
public sealed class MississippiProjectionTests
{
    /// <summary>
    ///     Ensures property initialization flows through the record constructor.
    /// </summary>
    [AllureEpic("Core")]
    [Fact]
    public void MississippiProjectionShouldAllowInitialization()
    {
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;
        ImmutableArray<byte> data = ImmutableArray.Create((byte)1, (byte)2);
        MississippiProjection projection = new()
        {
            CreationTime = timestamp,
            Data = data,
            DataContentType = "application/json",
            EventType = "evt",
            Hash = "hash",
            Id = "id-1",
            Source = "src",
        };
        Assert.Equal(timestamp, projection.CreationTime);
        Assert.Equal(data, projection.Data);
        Assert.Equal("application/json", projection.DataContentType);
        Assert.Equal("evt", projection.EventType);
        Assert.Equal("hash", projection.Hash);
        Assert.Equal("id-1", projection.Id);
        Assert.Equal("src", projection.Source);
    }

    /// <summary>
    ///     Ensures the record initializes with empty defaults and accepts populated values.
    /// </summary>
    [AllureEpic("Core")]
    [Fact]
    public void MississippiProjectionShouldInitializeDefaults()
    {
        MississippiProjection projection = new();
        Assert.Equal(ImmutableArray<byte>.Empty, projection.Data);
        Assert.Equal(string.Empty, projection.DataContentType);
        Assert.Equal(string.Empty, projection.EventType);
        Assert.Equal(string.Empty, projection.Hash);
        Assert.Equal(string.Empty, projection.Id);
        Assert.Equal(string.Empty, projection.Source);
        Assert.Null(projection.CreationTime);
    }
}