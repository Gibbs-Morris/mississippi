using System;
using System.Linq;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Cosmos.Mapping;
using Mississippi.EventSourcing.Cosmos.Storage;


namespace Mississippi.EventSourcing.Cosmos.Tests.Mapping;

/// <summary>
///     Test class for EventStorageToEventMapper functionality.
///     Contains unit tests to verify the behavior of event storage to event mapping operations.
/// </summary>
public class EventStorageToEventMapperTests
{
    /// <summary>
    ///     Verifies mapping populates all BrookEvent fields from storage model.
    /// </summary>
    [Fact]
    public void MapPopulatesAllFields()
    {
        // Arrange
        EventStorageModel input = new()
        {
            EventId = "evt-2",
            Source = "src",
            EventType = "etype",
            DataContentType = "application/octet-stream",
            Data = new byte[] { 9, 8, 7 },
            Time = DateTimeOffset.UtcNow,
        };
        EventStorageToEventMapper mapper = new();

        // Act
        BrookEvent result = mapper.Map(input);

        // Assert
        Assert.Equal(input.EventId, result.Id);
        Assert.Equal(input.Source, result.Source);
        Assert.Equal(input.EventType, result.Type);
        Assert.Equal(input.DataContentType, result.DataContentType);
        Assert.Equal(input.Data, result.Data.ToArray());
        Assert.Equal(input.Time, result.Time);
    }
}