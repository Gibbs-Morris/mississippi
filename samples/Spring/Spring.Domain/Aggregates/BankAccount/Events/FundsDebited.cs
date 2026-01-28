using System;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Spring.Domain.Aggregates.BankAccount.Events;

/// <summary>
///     Event raised when funds are debited from a bank account for an outgoing transfer.
/// </summary>
/// <remarks>
///     This event differs from <see cref="CashWithdrawn" /> in that it represents
///     an internal transfer to another account rather than an external cash withdrawal.
/// </remarks>
[EventStorageName("SPRING", "BANKING", "FUNDSDEBITED")]
[GenerateSerializer]
[Alias("Spring.Domain.BankAccount.Events.FundsDebited")]
internal sealed record FundsDebited
{
    /// <summary>
    ///     Gets the amount debited.
    /// </summary>
    [Id(0)]
    public decimal Amount { get; init; }

    /// <summary>
    ///     Gets the destination account ID for the transfer.
    /// </summary>
    [Id(2)]
    public required Guid DestinationAccountId { get; init; }

    /// <summary>
    ///     Gets the correlation ID linking this debit to the transfer saga.
    /// </summary>
    [Id(1)]
    public required Guid TransferCorrelationId { get; init; }
}