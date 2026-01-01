using System.Collections.Generic;
using System.Linq;

using Crescent.NewModel.Chat.Commands;
using Crescent.NewModel.Chat.Events;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Crescent.NewModel.Chat.Handlers;

/// <summary>
///     Command handler for deleting a message from the chat.
/// </summary>
internal sealed class DeleteMessageHandler : CommandHandler<DeleteMessage, ChatState>
{
    /// <inheritdoc />
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        DeleteMessage command,
        ChatState? state
    )
    {
        // Ensure chat exists
        if (state?.IsCreated != true)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(AggregateErrorCodes.NotFound, "Chat does not exist.");
        }

        // Validate command
        if (string.IsNullOrWhiteSpace(command.MessageId))
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "Message ID is required.");
        }

        // Find the message
        ChatMessage? existingMessage = state.Messages.FirstOrDefault(m => m.MessageId == command.MessageId);
        if (existingMessage is null)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.NotFound,
                $"Message with ID '{command.MessageId}' not found.");
        }

        return OperationResult.Ok<IReadOnlyList<object>>(
            new object[]
            {
                new MessageDeleted
                {
                    MessageId = command.MessageId,
                },
            });
    }
}