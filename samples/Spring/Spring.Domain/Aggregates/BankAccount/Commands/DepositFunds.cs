using Mississippi.Sdk.Generators.Abstractions;

using Orleans;


namespace Spring.Domain.Aggregates.BankAccount.Commands;

/// <summary>
///     Command to deposit funds into a bank account.
/// </summary>
[GenerateCommand(Route = "deposit")]
[GenerateSerializer]
[Alias("Spring.Domain.BankAccount.Commands.DepositFunds")]
public sealed record DepositFunds
{
    /// <summary>
    ///     Gets the amount to deposit.
    /// </summary>
    [Id(0)]
    public decimal Amount { get; init; }
}