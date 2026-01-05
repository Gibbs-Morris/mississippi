using System;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Cascade.Domain.Channel.Events;

/// <summary>
///     Event raised when a channel is archived.
/// </summary>
[EventStorageName("CASCADE", "CHAT", "CHANNELARCHIVED", version: 1)]
[GenerateSerializer]
[Alias("Cascade.Domain.Channel.Events.ChannelArchived")]
internal sealed record ChannelArchived
{
    /// <summary>
    ///     Gets the timestamp when the channel was archived.
    /// </summary>
    [Id(1)]
    public required DateTimeOffset ArchivedAt { get; init; }

    /// <summary>
    ///     Gets the user ID of the person who archived the channel.
    /// </summary>
    [Id(0)]
    public required string ArchivedBy { get; init; }
}