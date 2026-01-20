using Mississippi.Common.Abstractions.Attributes;
using Mississippi.Reservoir.Abstractions.Actions;


namespace Spring.Client.Features.BankAccountAggregate.Actions;

/// <summary>
///     Action dispatched to open a new bank account.
/// </summary>
/// <remarks>
///     Derived from domain command: <c>Spring.Domain.Aggregates.BankAccount.Commands.OpenAccount</c>.
/// </remarks>
/// <param name="AccountId">The aggregate ID to create.</param>
/// <param name="HolderName">The name of the account holder.</param>
/// <param name="InitialDeposit">The initial deposit amount.</param>
[PendingSourceGenerator]
internal sealed record OpenAccountAction(string AccountId, string HolderName, decimal InitialDeposit) : IAction;
