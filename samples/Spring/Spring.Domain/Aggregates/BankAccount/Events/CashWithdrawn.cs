using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Spring.Domain.Aggregates.BankAccount.Events;

/// <summary>
///     Event raised when cash is withdrawn from a bank account to an external destination.
/// </summary>
[EventStorageName("SPRING", "BANKING", "CASHWITHDRAWN")]
[GenerateSerializer]
[Alias("Spring.Domain.BankAccount.Events.CashWithdrawn")]
internal sealed record CashWithdrawn
{
    /// <summary>
    ///     Gets the amount withdrawn.
    /// </summary>
    [Id(0)]
    public decimal Amount { get; init; }
}