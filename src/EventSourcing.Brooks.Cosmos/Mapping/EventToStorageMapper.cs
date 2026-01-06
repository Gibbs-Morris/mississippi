using System;
using System.Linq;

using Mississippi.Common.Abstractions.Mapping;
using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Brooks.Cosmos.Storage;


namespace Mississippi.EventSourcing.Brooks.Cosmos.Mapping;

/// <summary>
///     Maps brook events to event storage models.
/// </summary>
internal class EventToStorageMapper : IMapper<BrookEvent, EventStorageModel>
{
    /// <summary>
    ///     Maps a brook event to an event storage model.
    /// </summary>
    /// <param name="input">The brook event to map.</param>
    /// <returns>The mapped event storage model.</returns>
    public EventStorageModel Map(
        BrookEvent input
    ) =>
        new()
        {
            EventId = input.Id ?? string.Empty,
            Source = input.Source,
            EventType = input.EventType ?? string.Empty,
            DataContentType = input.DataContentType,
            Data = input.Data.ToArray(),
            Time = input.Time ?? DateTimeOffset.UtcNow,
        };
}