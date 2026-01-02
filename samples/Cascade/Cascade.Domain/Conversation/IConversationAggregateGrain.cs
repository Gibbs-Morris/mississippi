// <copyright file="IConversationAggregateGrain.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System.Threading.Tasks;

using Mississippi.EventSourcing.Aggregates.Abstractions;

using Orleans;


namespace Cascade.Domain.Conversation;

/// <summary>
///     Grain interface for the conversation aggregate.
///     Exposes domain operations for managing chat conversations (message threads).
/// </summary>
[Alias("Cascade.Domain.Conversation.IConversationAggregateGrain")]
internal interface IConversationAggregateGrain : IAggregateGrain
{
    /// <summary>
    ///     Deletes a message from the conversation.
    /// </summary>
    /// <param name="messageId">The message identifier.</param>
    /// <param name="deletedBy">The user ID of the person deleting.</param>
    /// <returns>The operation result.</returns>
    [Alias("DeleteMessage")]
    Task<OperationResult> DeleteMessageAsync(
        string messageId,
        string deletedBy
    );

    /// <summary>
    ///     Edits a message in the conversation.
    /// </summary>
    /// <param name="messageId">The message identifier.</param>
    /// <param name="newContent">The new message content.</param>
    /// <param name="editedBy">The user ID of the editor.</param>
    /// <returns>The operation result.</returns>
    [Alias("EditMessage")]
    Task<OperationResult> EditMessageAsync(
        string messageId,
        string newContent,
        string editedBy
    );

    /// <summary>
    ///     Sends a message in the conversation.
    /// </summary>
    /// <param name="messageId">The message identifier.</param>
    /// <param name="content">The message content.</param>
    /// <param name="sentBy">The user ID of the sender.</param>
    /// <returns>The operation result.</returns>
    [Alias("SendMessage")]
    Task<OperationResult> SendMessageAsync(
        string messageId,
        string content,
        string sentBy
    );

    /// <summary>
    ///     Starts a new conversation.
    /// </summary>
    /// <param name="conversationId">The conversation identifier.</param>
    /// <param name="channelId">The channel identifier for the conversation.</param>
    /// <returns>The operation result.</returns>
    [Alias("Start")]
    Task<OperationResult> StartAsync(
        string conversationId,
        string channelId
    );
}