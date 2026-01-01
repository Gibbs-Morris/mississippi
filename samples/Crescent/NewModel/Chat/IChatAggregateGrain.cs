using System.Threading.Tasks;

using Mississippi.EventSourcing.Aggregates.Abstractions;

using Orleans;


namespace Crescent.NewModel.Chat;

/// <summary>
///     Grain interface for the chat aggregate.
///     Exposes domain operations for managing a chat conversation.
/// </summary>
[Alias("Crescent.NewModel.Chat.IChatAggregateGrain")]
internal interface IChatAggregateGrain : IAggregateGrain
{
    /// <summary>
    ///     Adds a message to the chat.
    /// </summary>
    /// <param name="messageId">Unique identifier for the message.</param>
    /// <param name="content">The message content.</param>
    /// <param name="author">The author of the message.</param>
    /// <returns>The operation result.</returns>
    [Alias("AddMessage")]
    Task<OperationResult> AddMessageAsync(
        string messageId,
        string content,
        string author
    );

    /// <summary>
    ///     Creates a new chat with the specified name.
    /// </summary>
    /// <param name="name">The name for the chat.</param>
    /// <returns>The operation result.</returns>
    [Alias("Create")]
    Task<OperationResult> CreateAsync(
        string name
    );

    /// <summary>
    ///     Deletes a message from the chat.
    /// </summary>
    /// <param name="messageId">The identifier of the message to delete.</param>
    /// <returns>The operation result.</returns>
    [Alias("DeleteMessage")]
    Task<OperationResult> DeleteMessageAsync(
        string messageId
    );

    /// <summary>
    ///     Edits an existing message in the chat.
    /// </summary>
    /// <param name="messageId">The identifier of the message to edit.</param>
    /// <param name="newContent">The new content for the message.</param>
    /// <returns>The operation result.</returns>
    [Alias("EditMessage")]
    Task<OperationResult> EditMessageAsync(
        string messageId,
        string newContent
    );
}
