using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Spring.Domain.Aggregates.BankAccount.Events;

/// <summary>
///     Event raised when funds are withdrawn from a bank account.
///     This is a general withdrawal event covering any type of outgoing funds.
/// </summary>
[EventStorageName("SPRING", "BANKING", "FUNDSWITHDRAWN")]
[GenerateSerializer]
[Alias("Spring.Domain.BankAccount.Events.FundsWithdrawn")]
internal sealed record FundsWithdrawn
{
    /// <summary>
    ///     Gets the amount withdrawn.
    /// </summary>
    [Id(0)]
    public decimal Amount { get; init; }
}