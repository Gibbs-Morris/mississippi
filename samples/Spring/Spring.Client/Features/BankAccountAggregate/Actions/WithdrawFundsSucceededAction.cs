#if false // Replaced by source generator: CommandClientActionsGenerator
using System;

using Mississippi.Inlet.Blazor.WebAssembly.Abstractions.Actions;
using Mississippi.Inlet.Generators.Abstractions;


namespace Spring.Client.Features.BankAccountAggregate.Actions;

/// <summary>
///     Action dispatched when a WithdrawFunds command completes successfully.
/// </summary>
/// <param name="CommandId">The unique command invocation identifier.</param>
/// <param name="Timestamp">The timestamp when the command completed.</param>
[PendingSourceGenerator]
internal sealed record WithdrawFundsSucceededAction(string CommandId, DateTimeOffset Timestamp)
    : ICommandSucceededAction<WithdrawFundsSucceededAction>
{
    /// <inheritdoc />
    public static WithdrawFundsSucceededAction Create(
        string commandId,
        DateTimeOffset timestamp
    ) =>
        new(commandId, timestamp);
}
#endif