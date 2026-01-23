using Mississippi.Sdk.Generators.Abstractions;

using Orleans;


namespace Spring.Domain.Aggregates.BankAccount.Commands;

/// <summary>
///     Command to withdraw funds from a bank account.
/// </summary>
[GenerateCommand(Route = "withdraw")]
[GenerateSerializer]
[Alias("Spring.Domain.BankAccount.Commands.WithdrawFunds")]
public sealed record WithdrawFunds
{
    /// <summary>
    ///     Gets the amount to withdraw.
    /// </summary>
    [Id(0)]
    public decimal Amount { get; init; }
}