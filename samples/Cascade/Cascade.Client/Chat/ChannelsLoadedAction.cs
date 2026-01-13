using System.Collections.Generic;

using Mississippi.Reservoir.Abstractions.Actions;


namespace Cascade.Client.Chat;

/// <summary>
///     Action dispatched when channels are loaded from the server projection.
/// </summary>
/// <param name="Channels">The list of channels the user belongs to.</param>
internal sealed record ChannelsLoadedAction(IReadOnlyList<ChannelInfo> Channels) : IAction;