using Mississippi.Brooks.Abstractions.Attributes;

using Orleans;


namespace Mississippi.Spring.Domain.Aggregates.BankAccount.Events;

/// <summary>
///     Event raised when funds are deposited into a bank account.
/// </summary>
[EventStorageName("SPRING", "BANKING", "FUNDSDEPOSITED")]
[GenerateSerializer]
[Alias("Mississippi.Spring.Domain.Aggregates.BankAccount.Events.FundsDeposited")]
internal sealed record FundsDeposited
{
    /// <summary>
    ///     Gets the amount deposited.
    /// </summary>
    [Id(0)]
    public decimal Amount { get; init; }
}