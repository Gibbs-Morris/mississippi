using System;

using Mississippi.Brooks.Abstractions.Streaming;
using Mississippi.DomainModeling.Abstractions;
using Mississippi.DomainModeling.Abstractions.Subscriptions;

using Orleans;
using Orleans.Streams;


namespace Mississippi.DomainModeling.Runtime.Subscriptions;

/// <summary>
///     Represents an active projection subscription within a <see cref="UxProjectionSubscriptionGrain" />.
/// </summary>
/// <remarks>
///     The <see cref="StreamHandle" /> is not serialized because <see cref="StreamSubscriptionHandle{T}" />
///     is not serializable. It is rehydrated when the grain activates by re-subscribing to the stream.
/// </remarks>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.UxProjections.ActiveSubscription")]
internal sealed class ActiveSubscription
{
    /// <summary>
    ///     Gets the projection key derived from the subscription request.
    /// </summary>
    [Id(1)]
    public required UxProjectionKey ProjectionKey { get; init; }

    /// <summary>
    ///     Gets the original subscription request from the client.
    /// </summary>
    [Id(0)]
    public required UxProjectionSubscriptionRequest Request { get; init; }

    /// <summary>
    ///     Gets or sets the stream subscription handle.
    /// </summary>
    /// <remarks>
    ///     This is not serialized and must be rehydrated on grain activation.
    /// </remarks>
    [field: NonSerialized]
    public StreamSubscriptionHandle<BrookCursorMovedEvent>? StreamHandle { get; set; }
}