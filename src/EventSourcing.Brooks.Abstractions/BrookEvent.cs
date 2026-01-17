using System;
using System.Collections.Immutable;

using Orleans;


namespace Mississippi.EventSourcing.Brooks.Abstractions;

/// <summary>
///     Represents an event used by the Mississippi event-sourcing subsystem.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.Brooks.Abstractions.BrookEvent")]
public sealed record BrookEvent
{
    /// <summary>
    ///     Gets the raw event payload.
    /// </summary>
    /// <remarks>
    ///     The application receiving the event is responsible for interpreting the
    ///     payload according to <see cref="DataContentType" />.
    /// </remarks>
    [Id(5)]
    public ImmutableArray<byte> Data { get; init; } = ImmutableArray<byte>.Empty;

    /// <summary>
    ///     Gets the MIME type that describes the <see cref="Data" /> payload.
    ///     Examples include <c>application/octet-stream</c> or <c>application/json</c>.
    /// </summary>
    [Id(4)]
    public string DataContentType { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the size of the <see cref="Data" /> payload in bytes.
    /// </summary>
    /// <remarks>
    ///     This denormalized field enables efficient queries for event size
    ///     without deserializing the payload.
    /// </remarks>
    [Id(6)]
    public long DataSizeBytes { get; init; }

    /// <summary>
    ///     Gets the semantic event type used to interpret and deserialize the payload downstream.
    /// </summary>
    [Id(0)]
    public string EventType { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the unique identifier for the event instance.
    /// </summary>
    [Id(2)]
    public string Id { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the logical source (typically a brook name) that produced the event.
    /// </summary>
    [Id(1)]
    public string Source { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the timestamp indicating when the event occurred.
    /// </summary>
    [Id(3)]
    public DateTimeOffset? Time { get; init; }
}