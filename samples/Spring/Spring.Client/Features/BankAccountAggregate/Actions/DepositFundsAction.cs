#if FALSE // Replaced by source generator: CommandClientActionsGenerator
using Mississippi.Inlet.Blazor.WebAssembly.Abstractions.Actions;
using Mississippi.Sdk.Generators.Abstractions;


namespace Spring.Client.Features.BankAccountAggregate.Actions;

/// <summary>
///     Action dispatched to deposit funds into a bank account.
/// </summary>
/// <remarks>
///     Derived from domain command: <c>Spring.Domain.Aggregates.BankAccount.Commands.DepositFunds</c>.
/// </remarks>
/// <param name="EntityId">The target entity ID.</param>
/// <param name="Amount">The amount to deposit.</param>
[PendingSourceGenerator]
internal sealed record DepositFundsAction(string EntityId, decimal Amount) : ICommandAction;
#endif