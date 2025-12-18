using System;
using System.Collections.Generic;
using System.Collections.Immutable;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Serialization.Abstractions;


namespace Mississippi.EventSourcing.Aggregates;

/// <summary>
///     Default implementation of <see cref="IBrookEventConverter" /> that uses
///     <see cref="ISerializationProvider" /> for payload encoding and
///     <see cref="IEventTypeRegistry" /> for type resolution.
/// </summary>
internal sealed class BrookEventConverter : IBrookEventConverter
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BrookEventConverter" /> class.
    /// </summary>
    /// <param name="serializationProvider">The provider for event serialization and deserialization.</param>
    /// <param name="eventTypeRegistry">The registry for resolving event type names and CLR types.</param>
    public BrookEventConverter(
        ISerializationProvider serializationProvider,
        IEventTypeRegistry eventTypeRegistry
    )
    {
        SerializationProvider = serializationProvider ?? throw new ArgumentNullException(nameof(serializationProvider));
        EventTypeRegistry = eventTypeRegistry ?? throw new ArgumentNullException(nameof(eventTypeRegistry));
    }

    private IEventTypeRegistry EventTypeRegistry { get; }

    private ISerializationProvider SerializationProvider { get; }

    /// <inheritdoc />
    public object ToDomainEvent(
        BrookEvent brookEvent
    )
    {
        ArgumentNullException.ThrowIfNull(brookEvent);
        Type? eventType = EventTypeRegistry.ResolveType(brookEvent.EventType);
        if (eventType is null)
        {
            throw new InvalidOperationException(
                $"Cannot resolve event type '{brookEvent.EventType}'. " +
                "Ensure the event type is registered in the event type registry.");
        }

        return SerializationProvider.Deserialize(eventType, brookEvent.Data.AsMemory());
    }

    /// <inheritdoc />
    public ImmutableArray<BrookEvent> ToStorageEvents(
        BrookKey source,
        IReadOnlyList<object> domainEvents
    )
    {
        ArgumentNullException.ThrowIfNull(domainEvents);
        ImmutableArray<BrookEvent>.Builder builder = ImmutableArray.CreateBuilder<BrookEvent>(domainEvents.Count);
        foreach (object eventData in domainEvents)
        {
            Type eventType = eventData.GetType();
            string? eventTypeName = EventTypeRegistry.ResolveName(eventType);
            if (eventTypeName is null)
            {
                throw new InvalidOperationException(
                    $"Cannot resolve event name for type '{eventType.Name}'. " +
                    "Ensure the event type is registered in the event type registry.");
            }

            ReadOnlyMemory<byte> data = SerializationProvider.Serialize(eventData);
            builder.Add(
                new()
                {
                    Id = Guid.NewGuid().ToString("N"),
                    EventType = eventTypeName,
                    Source = source,
                    Data = data.ToArray().ToImmutableArray(),
                    DataContentType = SerializationProvider.Format,
                });
        }

        return builder.ToImmutable();
    }
}