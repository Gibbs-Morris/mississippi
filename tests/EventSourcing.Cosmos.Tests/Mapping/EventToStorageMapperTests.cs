using System.Collections.Immutable;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Cosmos.Mapping;
using Mississippi.EventSourcing.Cosmos.Storage;


namespace Mississippi.EventSourcing.Cosmos.Tests.Mapping;

/// <summary>
///     Test class for EventToStorageMapper functionality.
///     Contains unit tests to verify the behavior of event to storage mapping operations.
/// </summary>
public class EventToStorageMapperTests
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
            Type = "my-type",
            DataContentType = "application/json",
            Data = ImmutableArray.Create<byte>(1, 2, 3),
            Time = DateTimeOffset.UtcNow,
        };
        EventToStorageMapper mapper = new();

        // Act
        EventStorageModel result = mapper.Map(input);

        // Assert
        Assert.Equal(input.Id, result.EventId);
        Assert.Equal(input.Source, result.Source);
        Assert.Equal(input.Type, result.EventType);
        Assert.Equal(input.DataContentType, result.DataContentType);
        Assert.Equal(input.Data.ToArray(), result.Data);
        Assert.Equal(input.Time, result.Time);
    }
}