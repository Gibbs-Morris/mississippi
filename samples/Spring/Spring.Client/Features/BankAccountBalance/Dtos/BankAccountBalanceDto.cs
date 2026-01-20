using Mississippi.Common.Abstractions.Attributes;


namespace Spring.Client.Features.BankAccountBalance.Dtos;

/// <summary>
///     DTO for the BankAccountBalance projection data.
/// </summary>
/// <remarks>
///     Mirrors server DTO: <c>Spring.Server.Endpoints.Projections.BankAccountBalance.BankAccountBalanceDto</c>.
/// </remarks>
/// <param name="Balance">The current balance.</param>
/// <param name="HolderName">The account holder name.</param>
/// <param name="IsOpen">Whether the account is open.</param>
[PendingSourceGenerator]
internal sealed record BankAccountBalanceDto(decimal Balance, string HolderName, bool IsOpen);
