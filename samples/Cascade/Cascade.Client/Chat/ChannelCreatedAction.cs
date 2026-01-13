using Mississippi.Reservoir.Abstractions.Actions;


namespace Cascade.Client.Chat;

/// <summary>
///     Action dispatched when a channel is successfully created.
/// </summary>
/// <param name="Channel">The created channel information.</param>
internal sealed record ChannelCreatedAction(ChannelInfo Channel) : IAction;