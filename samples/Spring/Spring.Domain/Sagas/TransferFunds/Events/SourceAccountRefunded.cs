using System;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Spring.Domain.Sagas.TransferFunds.Events;

/// <summary>
///     Event raised when the source account is refunded after a failed transfer.
/// </summary>
[EventStorageName("SPRING", "BANKING", "SOURCEACCOUNTREFUNDED")]
[GenerateSerializer]
[Alias("Spring.Domain.Sagas.TransferFunds.Events.SourceAccountRefunded")]
internal sealed record SourceAccountRefunded
{
    /// <summary>
    ///     Gets the amount refunded.
    /// </summary>
    [Id(1)]
    public decimal Amount { get; init; }

    /// <summary>
    ///     Gets the reason for the refund.
    /// </summary>
    [Id(2)]
    public string? Reason { get; init; }

    /// <summary>
    ///     Gets the source account ID.
    /// </summary>
    [Id(0)]
    public required Guid SourceAccountId { get; init; }
}