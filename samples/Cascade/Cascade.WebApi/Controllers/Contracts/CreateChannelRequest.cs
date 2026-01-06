namespace Cascade.WebApi.Controllers.Contracts;

/// <summary>
///     Request to create a new channel.
/// </summary>
/// <param name="ChannelName">The name of the channel to create.</param>
public sealed record CreateChannelRequest(string ChannelName);
