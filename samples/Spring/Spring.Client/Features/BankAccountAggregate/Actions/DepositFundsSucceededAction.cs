#if FALSE // Replaced by source generator: CommandClientActionsGenerator
using System;

using Mississippi.Inlet.Blazor.WebAssembly.Abstractions.Actions;
using Mississippi.Sdk.Generators.Abstractions;


namespace Spring.Client.Features.BankAccountAggregate.Actions;

/// <summary>
///     Action dispatched when a DepositFunds command completes successfully.
/// </summary>
/// <param name="CommandId">The unique command invocation identifier.</param>
/// <param name="Timestamp">The timestamp when the command completed.</param>
[PendingSourceGenerator]
internal sealed record DepositFundsSucceededAction(string CommandId, DateTimeOffset Timestamp)
    : ICommandSucceededAction<DepositFundsSucceededAction>
{
    /// <inheritdoc />
    public static DepositFundsSucceededAction Create(
        string commandId,
        DateTimeOffset timestamp
    ) =>
        new(commandId, timestamp);
}
#endif