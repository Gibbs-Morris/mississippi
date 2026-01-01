using System;
using System.Collections.Generic;
using System.Linq;

using Crescent.NewModel.Chat.Commands;
using Crescent.NewModel.Chat.Events;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Crescent.NewModel.Chat.Handlers;

/// <summary>
///     Command handler for editing a message in the chat.
/// </summary>
internal sealed class EditMessageHandler : CommandHandler<EditMessage, ChatAggregate>
{
    /// <inheritdoc />
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        EditMessage command,
        ChatAggregate? state
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

        if (string.IsNullOrWhiteSpace(command.NewContent))
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "New message content is required.");
        }

        // Find the message
        ChatMessage? existingMessage = state.Messages.FirstOrDefault(m => m.MessageId == command.MessageId);
        if (existingMessage is null)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.NotFound,
                $"Message with ID '{command.MessageId}' not found.");
        }

        // Skip if content is the same
        if (existingMessage.Content == command.NewContent)
        {
            return OperationResult.Ok<IReadOnlyList<object>>([]);
        }

        return OperationResult.Ok<IReadOnlyList<object>>(
            new object[]
            {
                new MessageEdited
                {
                    MessageId = command.MessageId,
                    PreviousContent = existingMessage.Content,
                    NewContent = command.NewContent,
                    EditedAt = DateTimeOffset.UtcNow,
                },
            });
    }
}