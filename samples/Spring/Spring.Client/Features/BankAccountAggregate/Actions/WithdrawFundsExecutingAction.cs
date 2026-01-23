#if false // Replaced by source generator: CommandClientActionsGenerator
using System;

using Mississippi.Inlet.Blazor.WebAssembly.Abstractions.Actions;
using Mississippi.Inlet.Generators.Abstractions;


namespace Spring.Client.Features.BankAccountAggregate.Actions;

/// <summary>
///     Action dispatched when a WithdrawFunds command starts executing.
/// </summary>
/// <param name="CommandId">The unique command invocation identifier.</param>
/// <param name="CommandType">The name of the command type.</param>
/// <param name="Timestamp">The timestamp when the command started.</param>
[PendingSourceGenerator]
internal sealed record WithdrawFundsExecutingAction(string CommandId, string CommandType, DateTimeOffset Timestamp)
    : ICommandExecutingAction<WithdrawFundsExecutingAction>
{
    /// <inheritdoc />
    public static WithdrawFundsExecutingAction Create(
        string commandId,
        string commandType,
        DateTimeOffset timestamp
    ) =>
        new(commandId, commandType, timestamp);
}
#endif