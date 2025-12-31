using System;

using Mississippi.EventSourcing.Brooks.Abstractions;

using Orleans;


namespace Mississippi.EventSourcing.UxProjections.Abstractions.Subscriptions;

/// <summary>
///     Event published when a projection's version changes.
/// </summary>
/// <remarks>
///     <para>
///         This event is published via Orleans streams to notify subscribers that a projection
///         has a new version available. Subscribers should use the <see cref="ProjectionKey" />
///         and <see cref="NewVersion" /> to fetch the updated projection state via HTTP.
///     </para>
///     <para>
///         The event intentionally carries only notification metadata, not the projection data itself,
///         to keep WebSocket payloads small. Clients fetch the actual data via HTTP GET with ETag support.
///     </para>
/// </remarks>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.UxProjections.Abstractions.UxProjectionChangedEvent")]
public sealed record UxProjectionChangedEvent
{
    /// <summary>
    ///     Gets the key identifying the projection that changed.
    /// </summary>
    [Id(0)]
    public required UxProjectionKey ProjectionKey { get; init; }

    /// <summary>
    ///     Gets the new version of the projection.
    /// </summary>
    [Id(1)]
    public required BrookPosition NewVersion { get; init; }

    /// <summary>
    ///     Gets the timestamp when the change was detected.
    /// </summary>
    [Id(2)]
    public required DateTimeOffset Timestamp { get; init; }
}
