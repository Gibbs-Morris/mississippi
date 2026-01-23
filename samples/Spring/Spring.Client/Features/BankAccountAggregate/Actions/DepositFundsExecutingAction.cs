#if false // Replaced by source generator: CommandClientActionsGenerator
using System;

using Mississippi.Inlet.Blazor.WebAssembly.Abstractions.Actions;
using Mississippi.Sdk.Generators.Abstractions;


namespace Spring.Client.Features.BankAccountAggregate.Actions;

/// <summary>
///     Action dispatched when a DepositFunds command starts executing.
/// </summary>
/// <param name="CommandId">The unique command invocation identifier.</param>
/// <param name="CommandType">The name of the command type.</param>
/// <param name="Timestamp">The timestamp when the command started.</param>
[PendingSourceGenerator]
internal sealed record DepositFundsExecutingAction(string CommandId, string CommandType, DateTimeOffset Timestamp)
    : ICommandExecutingAction<DepositFundsExecutingAction>
{
    /// <inheritdoc />
    public static DepositFundsExecutingAction Create(
        string commandId,
        string commandType,
        DateTimeOffset timestamp
    ) =>
        new(commandId, commandType, timestamp);
}
#endif