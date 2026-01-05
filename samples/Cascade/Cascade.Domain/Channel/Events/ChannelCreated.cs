using System;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Cascade.Domain.Channel.Events;

/// <summary>
///     Event raised when a channel is created.
/// </summary>
[EventStorageName("CASCADE", "CHAT", "CHANNELCREATED")]
[GenerateSerializer]
[Alias("Cascade.Domain.Channel.Events.ChannelCreated")]
internal sealed record ChannelCreated
{
    /// <summary>
    ///     Gets the channel identifier.
    /// </summary>
    [Id(0)]
    public required string ChannelId { get; init; }

    /// <summary>
    ///     Gets the timestamp when the channel was created.
    /// </summary>
    [Id(3)]
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    ///     Gets the user ID of the channel creator.
    /// </summary>
    [Id(2)]
    public required string CreatedBy { get; init; }

    /// <summary>
    ///     Gets the channel name.
    /// </summary>
    [Id(1)]
    public required string Name { get; init; }
}