using System.Collections.Immutable;


namespace Mississippi.EventSourcing.Abstractions;

/// <summary>
///     Represents an event used by the Mississippi event-sourcing subsystem.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.Core.Idea.BrookEvent")]
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

    /// <summary>
    ///     Gets the semantic type of the event.
    /// </summary>
    [Id(0)]
    public string Type { get; init; } = string.Empty;
}