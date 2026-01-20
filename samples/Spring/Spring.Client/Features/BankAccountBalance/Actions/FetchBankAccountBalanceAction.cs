using Mississippi.Common.Abstractions.Attributes;
using Mississippi.Reservoir.Abstractions.Actions;


namespace Spring.Client.Features.BankAccountBalance.Actions;

/// <summary>
///     Action dispatched to fetch the BankAccountBalance projection data.
/// </summary>
/// <remarks>
///     Derived from domain projection: <c>Spring.Domain.Projections.BankAccountBalance.BankAccountBalanceProjection</c>.
/// </remarks>
/// <param name="BankAccountBalanceId">The bank account balance ID to fetch.</param>
[PendingSourceGenerator]
internal sealed record FetchBankAccountBalanceAction(string BankAccountBalanceId) : IAction;