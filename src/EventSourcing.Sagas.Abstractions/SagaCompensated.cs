using System;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Mississippi.EventSourcing.Sagas.Abstractions;

/// <summary>
///     Event emitted when a saga completes compensation.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.Sagas.Abstractions.SagaCompensated")]
[EventStorageName("MISSISSIPPI", "SAGAS", "SAGACOMPENSATED")]
public sealed record SagaCompensated
{
    /// <summary>
    ///     Gets the timestamp when compensation completed.
    /// </summary>
    [Id(0)]
    public required DateTimeOffset CompletedAt { get; init; }
}