using Mississippi.Core.Abstractions.Mapping;
using Mississippi.EventSourcing.Cosmos.Storage;

namespace Mississippi.EventSourcing.Cosmos.Mapping;

internal class EventDocumentToStorageMapper : IMapper<EventDocument, EventStorageModel>
{
    public EventStorageModel Map(EventDocument input)
    {
        return new EventStorageModel
        {
            EventId = input.EventId,
            Source = input.Source,
            EventType = input.EventType,
            DataContentType = input.DataContentType,
            Data = input.Data,
            Time = input.Time
        };
    }
}