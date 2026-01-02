using System.Threading.Tasks;

using Mississippi.EventSourcing.Aggregates.Abstractions;

using Orleans;


namespace Cascade.Domain.User;

/// <summary>
///     Grain interface for the user aggregate.
///     Exposes domain operations for managing user profiles.
/// </summary>
[Alias("Cascade.Domain.User.IUserAggregateGrain")]
internal interface IUserAggregateGrain : IAggregateGrain
{
    /// <summary>
    ///     Joins a channel.
    /// </summary>
    /// <param name="channelId">The channel to join.</param>
    /// <returns>The operation result.</returns>
    [Alias("JoinChannel")]
    Task<OperationResult> JoinChannelAsync(
        string channelId
    );

    /// <summary>
    ///     Leaves a channel.
    /// </summary>
    /// <param name="channelId">The channel to leave.</param>
    /// <returns>The operation result.</returns>
    [Alias("LeaveChannel")]
    Task<OperationResult> LeaveChannelAsync(
        string channelId
    );

    /// <summary>
    ///     Registers a new user.
    /// </summary>
    /// <param name="userId">The unique identifier for the user.</param>
    /// <param name="displayName">The user's display name.</param>
    /// <returns>The operation result.</returns>
    [Alias("Register")]
    Task<OperationResult> RegisterAsync(
        string userId,
        string displayName
    );

    /// <summary>
    ///     Sets the user's online status.
    /// </summary>
    /// <param name="isOnline">True if online, false if offline.</param>
    /// <returns>The operation result.</returns>
    [Alias("SetOnlineStatus")]
    Task<OperationResult> SetOnlineStatusAsync(
        bool isOnline
    );

    /// <summary>
    ///     Updates the user's display name.
    /// </summary>
    /// <param name="newDisplayName">The new display name.</param>
    /// <returns>The operation result.</returns>
    [Alias("UpdateDisplayName")]
    Task<OperationResult> UpdateDisplayNameAsync(
        string newDisplayName
    );
}