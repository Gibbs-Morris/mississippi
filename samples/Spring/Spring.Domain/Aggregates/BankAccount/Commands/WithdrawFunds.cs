using Mississippi.Inlet.Generators.Abstractions;

using Orleans;


namespace Mississippi.Spring.Domain.Aggregates.BankAccount.Commands;

/// <summary>
///     Command to withdraw funds from a bank account.
/// </summary>
[GenerateCommand(Route = "withdraw")]
[GenerateMcpToolMetadata(
    Description =
        "Withdraws funds from a bank account. Decreases the account balance by the specified amount. Fails if insufficient funds.",
    Title = "Withdraw Funds",
    Destructive = true,
    Idempotent = false,
    ReadOnly = false,
    OpenWorld = false)]
[GenerateSerializer]
[Alias("Mississippi.Spring.Domain.Aggregates.BankAccount.Commands.WithdrawFunds")]
public sealed record WithdrawFunds
{
    /// <summary>
    ///     Gets the amount to withdraw.
    /// </summary>
    [Id(0)]
    [GenerateMcpParameterDescription(
        "The amount to withdraw in the account currency. Must be greater than zero and not exceed the current balance.")]
    public decimal Amount { get; init; }
}