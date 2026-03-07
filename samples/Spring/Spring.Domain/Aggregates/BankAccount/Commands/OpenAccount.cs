using Mississippi.Inlet.Generators.Abstractions;

using Orleans;


namespace MississippiSamples.Spring.Domain.Aggregates.BankAccount.Commands;

/// <summary>
///     Command to open a new bank account.
/// </summary>
/// <param name="HolderName">The name of the account holder.</param>
/// <param name="InitialDeposit">The initial deposit amount. Defaults to 0.</param>
[GenerateCommand(Route = "open")]
[GenerateMcpToolMetadata(
    Title = "Open Bank Account",
    Description = "Opens a new bank account for the specified holder with an optional initial deposit.",
    Destructive = false,
    Idempotent = false,
    ReadOnly = false,
    OpenWorld = false)]
[GenerateSerializer]
[Alias("MississippiSamples.Spring.Domain.Aggregates.BankAccount.Commands.OpenAccount")]
public sealed record OpenAccount(
    [property: Id(0)]
    [GenerateMcpParameterDescription("The full name of the account holder.")]
    string HolderName,
    [property: Id(1)]
    [GenerateMcpParameterDescription("The initial deposit amount. Defaults to zero if not specified.")]
    decimal InitialDeposit = 0
);