// <copyright file="SendMessageHandler.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;

using Cascade.Domain.Conversation.Commands;
using Cascade.Domain.Conversation.Events;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Cascade.Domain.Conversation.Handlers;

/// <summary>
///     Handles the <see cref="SendMessage" /> command.
/// </summary>
internal sealed class SendMessageHandler : CommandHandler<SendMessage, ConversationState>
{
    /// <inheritdoc />
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        SendMessage command,
        ConversationState? state
    )
    {
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

        if (string.IsNullOrWhiteSpace(command.SentBy))
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "Sender user ID is required.");
        }

        if (state is not { IsStarted: true })
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidState,
                "Conversation has not been started.");
        }

        return OperationResult.Ok<IReadOnlyList<object>>(
        [
            new MessageSent
            {
                MessageId = command.MessageId,
                Content = command.Content,
                SentBy = command.SentBy,
                SentAt = DateTimeOffset.UtcNow,
            },
        ]);
    }
}