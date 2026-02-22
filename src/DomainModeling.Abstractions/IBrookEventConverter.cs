using System;
using System.Collections.Generic;
using System.Collections.Immutable;

using Mississippi.EventSourcing.Brooks.Abstractions;


namespace Mississippi.EventSourcing.Aggregates.Abstractions;

/// <summary>
///     Provides bidirectional conversion between domain events and <see cref="BrookEvent" /> storage format.
/// </summary>
/// <remarks>
///     <para>
///         This converter encapsulates the serialization, type resolution, and metadata generation
///         required to persist domain events as <see cref="BrookEvent" /> records and to reconstruct
///         domain events during aggregate hydration or projection processing.
///     </para>
///     <para>
///         The converter uses <see cref="IEventTypeRegistry" /> for stable event type name resolution
///         and a serialization provider for payload encoding/decoding.
///     </para>
/// </remarks>
public interface IBrookEventConverter
{
    /// <summary>
    ///     Converts a domain event back to its strongly-typed representation.
    /// </summary>
    /// <param name="brookEvent">The storage event to convert.</param>
    /// <returns>The deserialized domain event object.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the event type cannot be resolved from the registry.
    /// </exception>
    object ToDomainEvent(
        BrookEvent brookEvent
    );

    /// <summary>
    ///     Converts a collection of domain events to <see cref="BrookEvent" /> storage format.
    /// </summary>
    /// <param name="source">The source brook key identifying the event stream.</param>
    /// <param name="domainEvents">The domain events to convert.</param>
    /// <returns>An immutable array of <see cref="BrookEvent" /> records ready for persistence.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when an event type is not registered in the event type registry.
    /// </exception>
    ImmutableArray<BrookEvent> ToStorageEvents(
        BrookKey source,
        IReadOnlyList<object> domainEvents
    );
}