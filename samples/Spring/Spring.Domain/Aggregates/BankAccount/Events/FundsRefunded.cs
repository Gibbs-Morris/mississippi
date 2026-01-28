using System;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Spring.Domain.Aggregates.BankAccount.Events;

/// <summary>
///     Event raised when a transfer is rolled back and funds are refunded to the source account.
/// </summary>
/// <remarks>
///     This event is raised during saga compensation when a transfer fails after the
///     source account was debited but before or during the credit to the destination account.
/// </remarks>
[EventStorageName("SPRING", "BANKING", "FUNDSREFUNDED")]
[GenerateSerializer]
[Alias("Spring.Domain.BankAccount.Events.FundsRefunded")]
internal sealed record FundsRefunded
{
    /// <summary>
    ///     Gets the amount refunded.
    /// </summary>
    [Id(0)]
    public decimal Amount { get; init; }

    /// <summary>
    ///     Gets the reason for the refund.
    /// </summary>
    [Id(2)]
    public string? Reason { get; init; }

    /// <summary>
    ///     Gets the correlation ID linking this refund to the original transfer saga.
    /// </summary>
    [Id(1)]
    public required Guid TransferCorrelationId { get; init; }
}