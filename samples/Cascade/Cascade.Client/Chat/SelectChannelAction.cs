using Mississippi.Reservoir.Abstractions.Actions;


namespace Cascade.Client.Chat;

/// <summary>
///     Action dispatched when a channel is selected.
/// </summary>
/// <param name="ChannelId">The selected channel's ID.</param>
internal sealed record SelectChannelAction(string ChannelId) : IAction;