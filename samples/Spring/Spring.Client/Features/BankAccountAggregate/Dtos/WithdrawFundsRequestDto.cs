using Mississippi.Common.Abstractions.Attributes;


namespace Spring.Client.Features.BankAccountAggregate.Dtos;

/// <summary>
///     Request DTO for the WithdrawFunds command.
/// </summary>
/// <remarks>
///     Mirrors server DTO: <c>Spring.Server.Endpoints.Commands.BankAccountAggregate.WithdrawFundsDto</c>.
/// </remarks>
/// <param name="Amount">The amount to withdraw.</param>
[PendingSourceGenerator]
internal sealed record WithdrawFundsRequestDto(decimal Amount);