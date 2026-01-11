using System;
using System.Collections.Generic;
using System.Linq;

using Cascade.Domain.Conversation.Commands;
using Cascade.Domain.Conversation.Events;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Cascade.Domain.Conversation.Handlers;

/// <summary>
///     Handles the <see cref="DeleteMessage" /> command.
/// </summary>
internal sealed class DeleteMessageHandler : CommandHandlerBase<DeleteMessage, ConversationAggregate>
{
    /// <inheritdoc />
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        DeleteMessage command,
        ConversationAggregate? state
    )
    {
        if (string.IsNullOrWhiteSpace(command.MessageId))
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "Message ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.DeletedBy))
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "Deleted by user ID is required.");
        }

        if (state is not { IsStarted: true })
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidState,
                "Conversation has not been started.");
        }

        Message? message = state.Messages.FirstOrDefault(m => m.MessageId == command.MessageId);
        if (message is null)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(AggregateErrorCodes.InvalidState, "Message not found.");
        }

        if (message.IsDeleted)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidState,
                "Message is already deleted.");
        }

        return OperationResult.Ok<IReadOnlyList<object>>(
        [
            new MessageDeleted
            {
                MessageId = command.MessageId,
                DeletedBy = command.DeletedBy,
                DeletedAt = DateTimeOffset.UtcNow,
            },
        ]);
    }
}