using System.Collections.Immutable;
using Mississippi.Core.Abstractions.Mapping;
using Mississippi.Core.Abstractions.Streams;
using Mississippi.EventSourcing.Cosmos.Storage;

namespace Mississippi.EventSourcing.Cosmos.Mapping;

public class EventStorageToEventMapper : IMapper<EventStorageModel, BrookEvent>
{
    public BrookEvent Map(EventStorageModel input)
    {
        return new BrookEvent
        {
            Id = input.EventId,
            Source = input.Source,
            Type = input.EventType,
            DataContentType = input.DataContentType,
            Data = input.Data.ToImmutableArray(),
            Time = input.Time
        };
    }
}