using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Cascade.Domain.Channel.Events;

/// <summary>
///     Event raised when a channel is renamed.
/// </summary>
[EventStorageName("CASCADE", "CHAT", "CHANNELRENAMED", version: 1)]
[GenerateSerializer]
[Alias("Cascade.Domain.Channel.Events.ChannelRenamed")]
internal sealed record ChannelRenamed
{
    /// <summary>
    ///     Gets the new channel name.
    /// </summary>
    [Id(1)]
    public required string NewName { get; init; }

    /// <summary>
    ///     Gets the old channel name.
    /// </summary>
    [Id(0)]
    public required string OldName { get; init; }
}