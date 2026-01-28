using System;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Spring.Domain.Sagas.TransferFunds.Events;

/// <summary>
///     Event raised when the destination account is successfully credited.
/// </summary>
[EventStorageName("SPRING", "BANKING", "DESTINATIONACCOUNTCREDITED")]
[GenerateSerializer]
[Alias("Spring.Domain.Sagas.TransferFunds.Events.DestinationAccountCredited")]
internal sealed record DestinationAccountCredited
{
    /// <summary>
    ///     Gets the amount credited.
    /// </summary>
    [Id(1)]
    public decimal Amount { get; init; }

    /// <summary>
    ///     Gets the destination account ID.
    /// </summary>
    [Id(0)]
    public required Guid DestinationAccountId { get; init; }
}