using System;
using System.Collections.Generic;

using Cascade.Contracts.Projections;


namespace Cascade.Client.Components.Organisms.MessageList;

/// <summary>
///     Represents a group of messages from the same sender within a time window.
/// </summary>
internal sealed record MessageGroup
{
    /// <summary>
    ///     Gets the timestamp of the first message in the group.
    /// </summary>
    public required DateTimeOffset FirstSentAt { get; init; }

    /// <summary>
    ///     Gets the messages in this group.
    /// </summary>
    public required IReadOnlyList<ChannelMessageItem> Messages { get; init; }

    /// <summary>
    ///     Gets the sender of this message group.
    /// </summary>
    public required string SentBy { get; init; }
}