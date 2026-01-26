using Mississippi.Inlet.Generators.Abstractions;

using Orleans;


namespace Spring.Domain.Aggregates.BankAccount.Commands;

/// <summary>
///     Command to deposit cash into a bank account from an external source.
/// </summary>
[GenerateCommand(Route = "deposit-cash")]
[GenerateSerializer]
[Alias("Spring.Domain.BankAccount.Commands.DepositCash")]
public sealed record DepositCash
{
    /// <summary>
    ///     Gets the amount to deposit.
    /// </summary>
    [Id(0)]
    public decimal Amount { get; init; }
}