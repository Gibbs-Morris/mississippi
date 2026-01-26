using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Spring.Domain.Aggregates.BankAccount.Events;

/// <summary>
///     Event raised when cash is deposited into a bank account from an external source.
/// </summary>
[EventStorageName("SPRING", "BANKING", "CASHDEPOSITED")]
[GenerateSerializer]
[Alias("Spring.Domain.BankAccount.Events.CashDeposited")]
internal sealed record CashDeposited
{
    /// <summary>
    ///     Gets the amount deposited.
    /// </summary>
    [Id(0)]
    public decimal Amount { get; init; }
}