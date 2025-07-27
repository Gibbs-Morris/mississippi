using Mississippi.Core.Abstractions.Mapping;
using Mississippi.Core.Abstractions.Streams;
using Mississippi.EventSourcing.Cosmos.Storage;

namespace Mississippi.EventSourcing.Cosmos.Mapping;

public class EventToStorageMapper : IMapper<BrookEvent, EventStorageModel>
{
    public EventStorageModel Map(BrookEvent input)
    {
        return new EventStorageModel
        {
            EventId = input.Id ?? string.Empty,
            Source = input.Source,
            EventType = input.Type ?? string.Empty,
            DataContentType = input.DataContentType,
            Data = input.Data.ToArray(),
            Time = input.Time ?? DateTimeOffset.UtcNow
        };
    }
}