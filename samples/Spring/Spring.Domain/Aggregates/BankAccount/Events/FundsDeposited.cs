using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Spring.Domain.Aggregates.BankAccount.Events;

/// <summary>
///     Event raised when funds are deposited into a bank account.
///     This is a general deposit event covering any type of incoming funds.
/// </summary>
[EventStorageName("SPRING", "BANKING", "FUNDSDEPOSITED")]
[GenerateSerializer]
[Alias("Spring.Domain.BankAccount.Events.FundsDeposited")]
internal sealed record FundsDeposited
{
    /// <summary>
    ///     Gets the amount deposited.
    /// </summary>
    [Id(0)]
    public decimal Amount { get; init; }
}