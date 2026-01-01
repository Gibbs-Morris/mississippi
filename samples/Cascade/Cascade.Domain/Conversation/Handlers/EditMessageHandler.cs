// <copyright file="EditMessageHandler.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;

using Cascade.Domain.Conversation.Commands;
using Cascade.Domain.Conversation.Events;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Cascade.Domain.Conversation.Handlers;

/// <summary>
///     Handles the <see cref="EditMessage" /> command.
/// </summary>
internal sealed class EditMessageHandler : CommandHandler<EditMessage, ConversationAggregate>
{
    /// <inheritdoc />
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        EditMessage command,
        ConversationAggregate? state
    )
    {
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
                "New content is required.");
        }

        if (string.IsNullOrWhiteSpace(command.EditedBy))
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "Editor user ID is required.");
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
                "Cannot edit a deleted message.");
        }

        if (message.SentBy != command.EditedBy)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "Only the sender can edit the message.");
        }

        return OperationResult.Ok<IReadOnlyList<object>>(
        [
            new MessageEdited
            {
                MessageId = command.MessageId,
                NewContent = command.NewContent,
                EditedBy = command.EditedBy,
                EditedAt = DateTimeOffset.UtcNow,
            },
        ]);
    }
}