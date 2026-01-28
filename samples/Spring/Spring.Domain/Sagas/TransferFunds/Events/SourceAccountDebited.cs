using System;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Spring.Domain.Sagas.TransferFunds.Events;

/// <summary>
///     Event raised when the source account is successfully debited.
/// </summary>
[EventStorageName("SPRING", "BANKING", "SOURCEACCOUNTDEBITED")]
[GenerateSerializer]
[Alias("Spring.Domain.Sagas.TransferFunds.Events.SourceAccountDebited")]
internal sealed record SourceAccountDebited
{
    /// <summary>
    ///     Gets the amount debited.
    /// </summary>
    [Id(1)]
    public decimal Amount { get; init; }

    /// <summary>
    ///     Gets the source account ID.
    /// </summary>
    [Id(0)]
    public required Guid SourceAccountId { get; init; }
}