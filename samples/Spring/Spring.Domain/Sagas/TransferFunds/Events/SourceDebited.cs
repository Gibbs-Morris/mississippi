using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Spring.Domain.Sagas.TransferFunds.Events;

/// <summary>
///     Event emitted when the source account has been successfully debited.
/// </summary>
[EventStorageName("SPRING", "BANKING", "SOURCEDEBITED")]
[GenerateSerializer]
[Alias("Spring.Domain.Sagas.TransferFunds.Events.SourceDebited")]
internal sealed record SourceDebited
{
    /// <summary>
    ///     Gets the amount that was debited.
    /// </summary>
    [Id(0)]
    public required decimal Amount { get; init; }
}