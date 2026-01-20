namespace Spring.Client.Features.BankAccountAggregate.Dtos;

/// <summary>
///     Request DTO for the DepositFunds command.
/// </summary>
/// <remarks>
///     Mirrors server DTO: <c>Spring.Server.Endpoints.Commands.BankAccountAggregate.DepositFundsDto</c>.
/// </remarks>
/// <param name="Amount">The amount to deposit.</param>
internal sealed record DepositFundsRequestDto(decimal Amount);
