using Mississippi.Common.Abstractions.Attributes;
using Mississippi.Reservoir.Abstractions.Actions;


namespace Spring.Client.Features.BankAccountAggregate.Actions;

/// <summary>
///     Action dispatched to deposit funds into a bank account.
/// </summary>
/// <remarks>
///     Derived from domain command: <c>Spring.Domain.Aggregates.BankAccount.Commands.DepositFunds</c>.
/// </remarks>
/// <param name="AccountId">The target account ID.</param>
/// <param name="Amount">The amount to deposit.</param>
[PendingSourceGenerator]
internal sealed record DepositFundsAction(string AccountId, decimal Amount) : IAction;
