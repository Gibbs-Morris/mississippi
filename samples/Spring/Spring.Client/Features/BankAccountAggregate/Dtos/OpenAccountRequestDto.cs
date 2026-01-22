using Mississippi.Sdk.Generators.Abstractions;


namespace Spring.Client.Features.BankAccountAggregate.Dtos;

/// <summary>
///     Request DTO for the OpenAccount command.
/// </summary>
/// <remarks>
///     Mirrors server DTO: <c>Spring.Server.Endpoints.Commands.BankAccountAggregate.OpenAccountDto</c>.
/// </remarks>
/// <param name="HolderName">The account holder name.</param>
/// <param name="InitialDeposit">The initial deposit amount.</param>
[PendingSourceGenerator]
internal sealed record OpenAccountRequestDto(string HolderName, decimal InitialDeposit);