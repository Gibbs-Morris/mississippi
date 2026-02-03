using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;

namespace Mississippi.EventSourcing.Sagas.Abstractions;

/// <summary>
///     Event emitted when a saga begins compensation.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.Sagas.Abstractions.SagaCompensating")]
[EventStorageName("MISSISSIPPI", "SAGAS", "SAGACOMPENSATING")]
public sealed record SagaCompensating
{
    /// <summary>
    ///     Gets the step index to start compensating from.
    /// </summary>
    [Id(0)]
    public required int FromStepIndex { get; init; }
}
