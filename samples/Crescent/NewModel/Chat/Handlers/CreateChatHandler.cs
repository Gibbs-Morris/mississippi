using System.Collections.Generic;

using Crescent.NewModel.Chat.Commands;
using Crescent.NewModel.Chat.Events;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Crescent.NewModel.Chat.Handlers;

/// <summary>
///     Command handler for creating a chat.
/// </summary>
internal sealed class CreateChatHandler : CommandHandler<CreateChat, ChatState>
{
    /// <inheritdoc />
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        CreateChat command,
        ChatState? state
    )
    {
        // Prevent re-creation
        if (state?.IsCreated == true)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.AlreadyExists,
                "Chat already exists.");
        }

        // Validate command
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "Chat name is required.");
        }

        return OperationResult.Ok<IReadOnlyList<object>>(
            new object[]
            {
                new ChatCreated
                {
                    Name = command.Name,
                },
            });
    }
}
