using Mississippi.Common.Abstractions.Mapping;
using Mississippi.EventSourcing.Brooks.Cosmos.Storage;


namespace Mississippi.EventSourcing.Brooks.Cosmos.Mapping;

/// <summary>
///     Maps event documents to event storage models.
/// </summary>
internal sealed class EventDocumentToStorageMapper : IMapper<EventDocument, EventStorageModel>
{
    /// <summary>
    ///     Maps an event document to an event storage model.
    /// </summary>
    /// <param name="input">The event document to map.</param>
    /// <returns>The mapped event storage model.</returns>
    public EventStorageModel Map(
        EventDocument input
    ) =>
        new()
        {
            EventId = input.EventId,
            Source = input.Source,
            EventType = input.EventType,
            DataContentType = input.DataContentType,
            Data = input.Data,
            Time = input.Time,
        };
}