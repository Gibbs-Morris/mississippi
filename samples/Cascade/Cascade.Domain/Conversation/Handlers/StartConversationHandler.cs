using System;
using System.Collections.Generic;

using Cascade.Domain.Conversation.Commands;
using Cascade.Domain.Conversation.Events;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Cascade.Domain.Conversation.Handlers;

/// <summary>
///     Handles the <see cref="StartConversation" /> command.
/// </summary>
internal sealed class StartConversationHandler : CommandHandler<StartConversation, ConversationAggregate>
{
    /// <inheritdoc />
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        StartConversation command,
        ConversationAggregate? state
    )
    {
        if (string.IsNullOrWhiteSpace(command.ConversationId))
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "Conversation ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.ChannelId))
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "Channel ID is required.");
        }

        if (state is { IsStarted: true })
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidState,
                "Conversation already started.");
        }

        return OperationResult.Ok<IReadOnlyList<object>>(
        [
            new ConversationStarted
            {
                ConversationId = command.ConversationId,
                ChannelId = command.ChannelId,
                StartedAt = DateTimeOffset.UtcNow,
            },
        ]);
    }
}