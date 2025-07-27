using System.Collections.Immutable;
using Mississippi.Core.Abstractions.Mapping;
using Mississippi.Core.Abstractions.Streams;
using Mississippi.EventSourcing.Cosmos.Storage;

namespace Mississippi.EventSourcing.Cosmos.Mapping;

/// <summary>
/// Maps event storage models to brook events.
/// </summary>
internal class EventStorageToEventMapper : IMapper<EventStorageModel, BrookEvent>
{
    /// <summary>
    /// Maps an event storage model to a brook event.
    /// </summary>
    /// <param name="input">The event storage model to map.</param>
    /// <returns>The mapped brook event.</returns>
    public BrookEvent Map(EventStorageModel input)
    {
        return new BrookEvent
        {
            Id = input.EventId,
            Source = input.Source ?? string.Empty,
            Type = input.EventType,
            DataContentType = input.DataContentType ?? string.Empty,
            Data = input.Data?.ToImmutableArray() ?? ImmutableArray<byte>.Empty,
            Time = input.Time,
        };
    }
}