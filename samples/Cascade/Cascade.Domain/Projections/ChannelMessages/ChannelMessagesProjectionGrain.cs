// Copyright (c) Gibbs-Morris. All rights reserved.

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;
using Mississippi.EventSourcing.UxProjections;
using Mississippi.EventSourcing.UxProjections.Abstractions;

using Orleans.Runtime;


namespace Cascade.Domain.Projections.ChannelMessages;

/// <summary>
///     UX projection grain providing cached, read-optimized access to
///     <see cref="ChannelMessagesProjection" /> state.
/// </summary>
/// <remarks>
///     <para>
///         This grain provides message history for a channel or conversation,
///         optimized for display in the chat message view UI.
///     </para>
///     <para>
///         The grain consumes the Conversation brook event stream and produces a
///         read-optimized projection for UX display purposes.
///     </para>
/// </remarks>
[BrookName("CASCADE", "CHAT", "CONVERSATION")]
internal sealed class ChannelMessagesProjectionGrain : UxProjectionGrainBase<ChannelMessagesProjection>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ChannelMessagesProjectionGrain" /> class.
    /// </summary>
    /// <param name="grainContext">The Orleans grain context.</param>
    /// <param name="uxProjectionGrainFactory">Factory for resolving UX projection grains and cursors.</param>
    /// <param name="logger">Logger instance.</param>
    public ChannelMessagesProjectionGrain(
        IGrainContext grainContext,
        IUxProjectionGrainFactory uxProjectionGrainFactory,
        ILogger<ChannelMessagesProjectionGrain> logger
    )
        : base(grainContext, uxProjectionGrainFactory, logger)
    {
    }
}