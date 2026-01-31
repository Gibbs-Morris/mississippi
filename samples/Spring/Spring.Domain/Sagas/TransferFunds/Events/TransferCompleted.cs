using System;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Spring.Domain.Sagas.TransferFunds.Events;

/// <summary>
///     Event emitted when the transfer has completed successfully.
/// </summary>
[EventStorageName("SPRING", "BANKING", "TRANSFERCOMPLETED")]
[GenerateSerializer]
[Alias("Spring.Domain.Sagas.TransferFunds.Events.TransferCompleted")]
internal sealed record TransferCompleted
{
    /// <summary>
    ///     Gets the timestamp when the transfer completed.
    /// </summary>
    [Id(0)]
    public required DateTimeOffset CompletedAt { get; init; }
}
