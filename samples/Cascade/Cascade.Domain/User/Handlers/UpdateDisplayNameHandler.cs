// <copyright file="UpdateDisplayNameHandler.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System.Collections.Generic;

using Cascade.Domain.User.Commands;
using Cascade.Domain.User.Events;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Cascade.Domain.User.Handlers;

/// <summary>
///     Command handler for updating a user's display name.
/// </summary>
internal sealed class UpdateDisplayNameHandler : CommandHandler<UpdateDisplayName, UserState>
{
    /// <inheritdoc />
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        UpdateDisplayName command,
        UserState? state
    )
    {
        if (state?.IsRegistered != true)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidState,
                "User must be registered before updating display name.");
        }

        if (string.IsNullOrWhiteSpace(command.NewDisplayName))
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "Display name cannot be empty.");
        }

        if (command.NewDisplayName == state.DisplayName)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "Display name is already set to this value.");
        }

        return OperationResult.Ok<IReadOnlyList<object>>(
            new object[]
            {
                new DisplayNameUpdated
                {
                    OldDisplayName = state.DisplayName,
                    NewDisplayName = command.NewDisplayName,
                },
            });
    }
}