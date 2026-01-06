namespace Cascade.WebApi.Controllers.Contracts;

/// <summary>
///     Response containing the created channel ID.
/// </summary>
/// <param name="ChannelId">The created channel ID.</param>
public sealed record CreateChannelResponse(string ChannelId);
