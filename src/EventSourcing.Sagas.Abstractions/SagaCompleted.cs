using System;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Mississippi.EventSourcing.Sagas.Abstractions;

/// <summary>
///     Event emitted when a saga completes successfully.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.Sagas.Abstractions.SagaCompleted")]
[EventStorageName("MISSISSIPPI", "SAGAS", "SAGACOMPLETED")]
public sealed record SagaCompleted
{
    /// <summary>
    ///     Gets the timestamp when the saga completed.
    /// </summary>
    [Id(0)]
    public required DateTimeOffset CompletedAt { get; init; }
}