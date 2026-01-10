using System.Collections.Immutable;

using Mississippi.Common.Abstractions.Mapping;
using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Brooks.Cosmos.Storage;


namespace Mississippi.EventSourcing.Brooks.Cosmos.Mapping;

/// <summary>
///     Maps event storage models to brook events.
/// </summary>
internal sealed class EventStorageToEventMapper : IMapper<EventStorageModel, BrookEvent>
{
    /// <summary>
    ///     Maps an event storage model to a brook event.
    /// </summary>
    /// <param name="input">The event storage model to map.</param>
    /// <returns>The mapped brook event.</returns>
    public BrookEvent Map(
        EventStorageModel input
    ) =>
        new()
        {
            Id = input.EventId,
            Source = input.Source ?? string.Empty,
            EventType = input.EventType,
            DataContentType = input.DataContentType ?? string.Empty,
            Data = input.Data?.ToImmutableArray() ?? ImmutableArray<byte>.Empty,
            Time = input.Time,
        };
}