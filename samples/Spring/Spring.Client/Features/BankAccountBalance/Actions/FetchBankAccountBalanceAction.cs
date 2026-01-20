using Mississippi.Common.Abstractions.Attributes;
using Mississippi.Reservoir.Abstractions.Actions;


namespace Spring.Client.Features.BankAccountBalance.Actions;

/// <summary>
///     Action dispatched to fetch the BankAccountBalance projection data.
/// </summary>
/// <remarks>
///     Derived from domain projection: <c>Spring.Domain.Projections.BankAccountBalance.BankAccountBalanceProjection</c>.
/// </remarks>
/// <param name="AccountId">The account ID to fetch the balance for.</param>
[PendingSourceGenerator]
internal sealed record FetchBankAccountBalanceAction(string AccountId) : IAction;
