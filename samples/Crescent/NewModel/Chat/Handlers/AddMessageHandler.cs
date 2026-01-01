using System;
using System.Collections.Generic;

using Crescent.NewModel.Chat.Commands;
using Crescent.NewModel.Chat.Events;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Crescent.NewModel.Chat.Handlers;

/// <summary>
///     Command handler for adding a message to the chat.
/// </summary>
internal sealed class AddMessageHandler : CommandHandler<AddMessage, ChatState>
{
    /// <inheritdoc />
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        AddMessage command,
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

        if (string.IsNullOrWhiteSpace(command.Content))
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "Message content is required.");
        }

        if (string.IsNullOrWhiteSpace(command.Author))
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "Message author is required.");
        }

        return OperationResult.Ok<IReadOnlyList<object>>(
            new object[]
            {
                new MessageAdded
                {
                    MessageId = command.MessageId,
                    Content = command.Content,
                    Author = command.Author,
                    CreatedAt = DateTimeOffset.UtcNow,
                },
            });
    }
}