using System;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;

namespace Spring.Domain.Aggregates.BankAccount.Events;

/// <summary>
///     Event raised when funds are received into a bank account from an incoming transfer.
/// </summary>
/// <remarks>
///     This event differs from <see cref="CashDeposited" /> in that it represents
///     an internal transfer from another account rather than an external cash deposit.
/// </remarks>
[EventStorageName("SPRING", "BANKING", "FUNDSRECEIVED")]
[GenerateSerializer]
[Alias("Spring.Domain.BankAccount.Events.FundsReceived")]
internal sealed record FundsReceived
{
    /// <summary>
    ///     Gets the amount received.
    /// </summary>
    [Id(0)]
    public decimal Amount { get; init; }

    /// <summary>
    ///     Gets the correlation ID linking this credit to the transfer saga.
    /// </summary>
    [Id(1)]
    public required Guid TransferCorrelationId { get; init; }

    /// <summary>
    ///     Gets the source account ID for the transfer.
    /// </summary>
    [Id(2)]
    public required Guid SourceAccountId { get; init; }
}
