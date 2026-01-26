using Mississippi.Inlet.Generators.Abstractions;

using Orleans;


namespace Spring.Domain.Aggregates.BankAccount.Commands;

/// <summary>
///     Command to withdraw cash from a bank account to an external destination.
/// </summary>
[GenerateCommand(Route = "withdraw-cash")]
[GenerateSerializer]
[Alias("Spring.Domain.BankAccount.Commands.WithdrawCash")]
public sealed record WithdrawCash
{
    /// <summary>
    ///     Gets the amount to withdraw.
    /// </summary>
    [Id(0)]
    public decimal Amount { get; init; }
}