using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Spring.Domain.Sagas.TransferFunds.Events;

/// <summary>
///     Event emitted when a transfer is initiated, capturing the input data.
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
    public required decimal Amount { get; init; }

    /// <summary>
    ///     Gets the destination account identifier.
    /// </summary>
    [Id(1)]
    public required string DestinationAccountId { get; init; }

    /// <summary>
    ///     Gets the source account identifier.
    /// </summary>
    [Id(0)]
    public required string SourceAccountId { get; init; }
}