using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Sdk.Generators.Abstractions;


namespace Spring.Client.Features.BankAccountAggregate.Actions;

/// <summary>
///     Action dispatched to withdraw funds from a bank account.
/// </summary>
/// <remarks>
///     Derived from domain command: <c>Spring.Domain.Aggregates.BankAccount.Commands.WithdrawFunds</c>.
/// </remarks>
/// <param name="EntityId">The target entity ID.</param>
/// <param name="Amount">The amount to withdraw.</param>
[PendingSourceGenerator]
internal sealed record WithdrawFundsAction(string EntityId, decimal Amount) : IAction;