using Mississippi.Sdk.Generators.Abstractions;

using Orleans;


namespace Spring.Domain.Aggregates.BankAccount.Commands;

/// <summary>
///     Command to open a new bank account.
/// </summary>
/// <param name="HolderName">The name of the account holder.</param>
/// <param name="InitialDeposit">The initial deposit amount. Defaults to 0.</param>
[GenerateCommand(Route = "open")]
[GenerateSerializer]
[Alias("Spring.Domain.BankAccount.Commands.OpenAccount")]
public sealed record OpenAccount([property: Id(0)] string HolderName, [property: Id(1)] decimal InitialDeposit = 0);