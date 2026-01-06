using System.Threading;
using System.Threading.Tasks;


namespace Cascade.Components.Services;

/// <summary>
///     Facade for chat operations that wraps grain calls or API calls.
/// </summary>
/// <remarks>
///     <para>
///         This service provides a clean abstraction layer between Blazor components
///         and the underlying communication mechanism (Orleans grains or HTTP API).
///     </para>
///     <para>
///         Implementations:
///         <list type="bullet">
///             <item>
///                 <term>Blazor Server</term>
///                 <description>Directly invokes Orleans grains via <c>IAggregateGrainFactory</c>.</description>
///             </item>
///             <item>
///                 <term>Blazor WASM</term>
///                 <description>Calls HTTP API endpoints that proxy to grains.</description>
///             </item>
///         </list>
///     </para>
/// </remarks>
public interface IChatService
{
    /// <summary>
    ///     Creates a new channel with the specified name.
    /// </summary>
    /// <param name="name">The channel name.</param>
    /// <param name="description">Optional channel description.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The ID of the created channel.</returns>
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
    Task SendMessageAsync(
        string channelId,
        string content,
        CancellationToken cancellationToken = default
    );
}