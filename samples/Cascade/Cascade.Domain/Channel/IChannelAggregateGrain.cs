using System.Threading.Tasks;

using Mississippi.EventSourcing.Aggregates.Abstractions;

using Orleans;


namespace Cascade.Domain.Channel;

/// <summary>
///     Grain interface for the channel aggregate.
///     Exposes domain operations for managing chat channels.
/// </summary>
[Alias("Cascade.Domain.Channel.IChannelAggregateGrain")]
internal interface IChannelAggregateGrain : IAggregateGrain
{
    /// <summary>
    ///     Adds a member to the channel.
    /// </summary>
    /// <param name="userId">The user ID of the member to add.</param>
    /// <returns>The operation result.</returns>
    [Alias("AddMember")]
    Task<OperationResult> AddMemberAsync(
        string userId
    );

    /// <summary>
    ///     Archives the channel.
    /// </summary>
    /// <param name="archivedBy">The user ID of the person archiving.</param>
    /// <returns>The operation result.</returns>
    [Alias("Archive")]
    Task<OperationResult> ArchiveAsync(
        string archivedBy
    );

    /// <summary>
    ///     Creates a new channel.
    /// </summary>
    /// <param name="channelId">The channel identifier.</param>
    /// <param name="name">The channel name.</param>
    /// <param name="createdBy">The user ID of the creator.</param>
    /// <returns>The operation result.</returns>
    [Alias("Create")]
    Task<OperationResult> CreateAsync(
        string channelId,
        string name,
        string createdBy
    );

    /// <summary>
    ///     Removes a member from the channel.
    /// </summary>
    /// <param name="userId">The user ID of the member to remove.</param>
    /// <returns>The operation result.</returns>
    [Alias("RemoveMember")]
    Task<OperationResult> RemoveMemberAsync(
        string userId
    );

    /// <summary>
    ///     Renames the channel.
    /// </summary>
    /// <param name="newName">The new channel name.</param>
    /// <returns>The operation result.</returns>
    [Alias("Rename")]
    Task<OperationResult> RenameAsync(
        string newName
    );
}