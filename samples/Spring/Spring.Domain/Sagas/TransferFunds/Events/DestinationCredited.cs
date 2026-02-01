using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Spring.Domain.Sagas.TransferFunds.Events;

/// <summary>
///     Event emitted when the destination account has been successfully credited.
/// </summary>
[EventStorageName("SPRING", "BANKING", "DESTINATIONCREDITED")]
[GenerateSerializer]
[Alias("Spring.Domain.Sagas.TransferFunds.Events.DestinationCredited")]
internal sealed record DestinationCredited
{
    /// <summary>
    ///     Gets the amount that was credited.
    /// </summary>
    [Id(0)]
    public required decimal Amount { get; init; }
}