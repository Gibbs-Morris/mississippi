using System;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Spring.Domain.Sagas.TransferFunds.Events;

/// <summary>
///     Event raised when a transfer saga is initiated.
/// </summary>
[EventStorageName("SPRING", "BANKING", "TRANSFERINITIATED")]
[GenerateSerializer]
[Alias("Spring.Domain.Sagas.TransferFunds.Events.TransferInitiated")]
internal sealed record TransferInitiated
{
    /// <summary>
    ///     Gets the amount to transfer.
    /// </summary>
    [Id(2)]
    public decimal Amount { get; init; }

    /// <summary>
    ///     Gets the destination account ID.
    /// </summary>
    [Id(1)]
    public required Guid DestinationAccountId { get; init; }

    /// <summary>
    ///     Gets the timestamp when the transfer was initiated.
    /// </summary>
    [Id(3)]
    public DateTimeOffset InitiatedAt { get; init; }

    /// <summary>
    ///     Gets the source account ID.
    /// </summary>
    [Id(0)]
    public required Guid SourceAccountId { get; init; }
}