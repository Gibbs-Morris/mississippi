using Mississippi.Common.Abstractions.Attributes;
using Mississippi.Reservoir.Abstractions.Actions;


namespace Spring.Client.Features.BankAccountAggregate.Actions;

/// <summary>
///     Action dispatched to withdraw funds from a bank account.
/// </summary>
/// <remarks>
///     Derived from domain command: <c>Spring.Domain.Aggregates.BankAccount.Commands.WithdrawFunds</c>.
/// </remarks>
/// <param name="AccountId">The target account ID.</param>
/// <param name="Amount">The amount to withdraw.</param>
[PendingSourceGenerator]
internal sealed record WithdrawFundsAction(string AccountId, decimal Amount) : IAction;
