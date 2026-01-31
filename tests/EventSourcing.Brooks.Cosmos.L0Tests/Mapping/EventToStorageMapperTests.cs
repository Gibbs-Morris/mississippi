using System;
using System.Collections.Immutable;
using System.Linq;

using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Brooks.Cosmos.Mapping;
using Mississippi.EventSourcing.Brooks.Cosmos.Storage;


namespace Mississippi.EventSourcing.Brooks.Cosmos.L0Tests.Mapping;

/// <summary>
///     Test class for EventToStorageMapper functionality.
///     Contains unit tests to verify the behavior of event to storage mapping operations.
/// </summary>
public sealed class EventToStorageMapperTests
{
    /// <summary>
    ///     Verifies mapping populates storage model from BrookEvent.
    /// </summary>
    [Fact]
    public void MapPopulatesAllFields()
    {
        // Arrange
        BrookEvent input = new()
        {
            Id = "evt-1",
            Source = "the-source",
            EventType = "my-type",
            DataContentType = "application/json",
            Data = ImmutableArray.Create<byte>(1, 2, 3),
            DataSizeBytes = 3,
            Time = DateTimeOffset.UtcNow,
        };
        EventToStorageMapper mapper = new();

        // Act
        EventStorageModel result = mapper.Map(input);

        // Assert
        Assert.Equal(input.Id, result.EventId);
        Assert.Equal(input.Source, result.Source);
        Assert.Equal(input.EventType, result.EventType);
        Assert.Equal(input.DataContentType, result.DataContentType);
        Assert.Equal(input.Data.ToArray(), result.Data);
        Assert.Equal(3, result.DataSizeBytes);
        Assert.Equal(input.Time, result.Time);
    }

    /// <summary>
    ///     Verifies mapping throws when event has no timestamp.
    /// </summary>
    [Fact]
    public void MapThrowsWhenTimeIsNull()
    {
        // Arrange
        BrookEvent input = new()
        {
            Id = "evt-1",
            Source = "the-source",
            EventType = "my-type",
            DataContentType = "application/json",
            Data = ImmutableArray.Create<byte>(1, 2, 3),
            DataSizeBytes = 3,
            Time = null,
        };
        EventToStorageMapper mapper = new();

        // Act & Assert
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => mapper.Map(input));
        Assert.Contains("evt-1", exception.Message, StringComparison.Ordinal);
        Assert.Contains("no timestamp", exception.Message, StringComparison.Ordinal);
    }
}