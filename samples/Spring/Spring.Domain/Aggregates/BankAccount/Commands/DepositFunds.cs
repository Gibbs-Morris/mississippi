using Mississippi.Inlet.Generators.Abstractions;

using Orleans;


namespace MississippiSamples.Spring.Domain.Aggregates.BankAccount.Commands;

/// <summary>
///     Command to deposit funds into a bank account.
/// </summary>
[GenerateCommand(Route = "deposit")]
[GenerateMcpToolMetadata(
    Title = "Deposit Funds",
    Description = "Deposits funds into a bank account. Increases the account balance by the specified amount.",
    Destructive = false,
    Idempotent = false,
    ReadOnly = false,
    OpenWorld = false)]
[GenerateSerializer]
[Alias("MississippiSamples.Spring.Domain.BankAccount.Commands.DepositFunds")]
public sealed record DepositFunds
{
    /// <summary>
    ///     Gets the amount to deposit.
    /// </summary>
    [Id(0)]
    [GenerateMcpParameterDescription("The amount to deposit in the account currency. Must be greater than zero.")]
    public decimal Amount { get; init; }
}