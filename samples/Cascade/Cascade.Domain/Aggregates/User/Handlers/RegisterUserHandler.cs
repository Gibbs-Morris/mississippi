using System;
using System.Collections.Generic;

using Cascade.Domain.Aggregates.User.Commands;
using Cascade.Domain.Aggregates.User.Events;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Cascade.Domain.Aggregates.User.Handlers;

/// <summary>
///     Command handler for registering a new user.
/// </summary>
internal sealed class RegisterUserHandler : CommandHandlerBase<RegisterUser, UserAggregate>
{
    /// <inheritdoc />
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        RegisterUser command,
        UserAggregate? state
    )
    {
        if (state?.IsRegistered == true)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidState,
                "User is already registered.");
        }

        if (string.IsNullOrWhiteSpace(command.UserId))
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "User ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.DisplayName))
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "Display name is required.");
        }

        return OperationResult.Ok<IReadOnlyList<object>>(
            new object[]
            {
                new UserRegistered
                {
                    UserId = command.UserId,
                    DisplayName = command.DisplayName,
                    RegisteredAt = DateTimeOffset.UtcNow,
                },
            });
    }
}