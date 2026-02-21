using Mississippi.Inlet.Generators.Abstractions;

using Orleans;


namespace Spring.Domain.Aggregates.BankAccount.Commands;

/// <summary>
///     Command to withdraw funds from a bank account.
/// </summary>
[GenerateCommand(Route = "withdraw")]
[GenerateMcpToolMetadata(
    Title = "Withdraw Funds",
    Description = "Withdraws funds from a bank account. Decreases the account balance by the specified amount. Fails if insufficient funds.",
    Destructive = true,
    Idempotent = false,
    ReadOnly = false,
    OpenWorld = false)]
[GenerateSerializer]
[Alias("Spring.Domain.BankAccount.Commands.WithdrawFunds")]
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