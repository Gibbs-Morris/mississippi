using System;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Mississippi.EventSourcing.Sagas.Abstractions;

/// <summary>
///     Event emitted when a saga input payload is captured.
/// </summary>
/// <typeparam name="TInput">The saga input type.</typeparam>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.Sagas.Abstractions.SagaInputProvided`1")]
[EventStorageName("MISSISSIPPI", "SAGAS", "SAGAINPUTPROVIDED")]
public sealed record SagaInputProvided<TInput>
{
    /// <summary>
    ///     Gets the saga input payload.
    /// </summary>
    [Id(1)]
    public required TInput Input { get; init; }

    /// <summary>
    ///     Gets the saga identifier.
    /// </summary>
    [Id(0)]
    public required Guid SagaId { get; init; }
}