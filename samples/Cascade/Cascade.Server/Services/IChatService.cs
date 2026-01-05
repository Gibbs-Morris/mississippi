using System.Threading;
using System.Threading.Tasks;


namespace Cascade.Server.Services;

/// <summary>
///     Facade for chat operations that wraps grain calls.
/// </summary>
/// <remarks>
///     <para>
///         This service provides a clean abstraction layer between Blazor components
///         and Orleans grains. It handles the grain resolution and operation result
///         processing, throwing descriptive exceptions on failure.
///     </para>
/// </remarks>
internal interface IChatService
{
    /// <summary>
    ///     Creates a new channel with the specified name.
    /// </summary>
    /// <param name="name">The channel name.</param>
    /// <param name="description">Optional channel description.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The ID of the created channel.</returns>
    /// <exception cref="ChatOperationException">When channel creation fails.</exception>
    Task<string> CreateChannelAsync(
        string name,
        string? description = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Joins the current user to a channel.
    /// </summary>
    /// <param name="channelId">The channel to join.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ChatOperationException">When joining the channel fails.</exception>
    Task JoinChannelAsync(
        string channelId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Removes the current user from a channel.
    /// </summary>
    /// <param name="channelId">The channel to leave.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ChatOperationException">When leaving the channel fails.</exception>
    Task LeaveChannelAsync(
        string channelId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Sends a message to a channel.
    /// </summary>
    /// <param name="channelId">The target channel.</param>
    /// <param name="content">The message content.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ChatOperationException">When sending the message fails.</exception>
    Task SendMessageAsync(
        string channelId,
        string content,
        CancellationToken cancellationToken = default
    );
}