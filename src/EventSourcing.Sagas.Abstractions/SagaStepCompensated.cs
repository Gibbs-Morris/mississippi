using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;

namespace Mississippi.EventSourcing.Sagas.Abstractions;

/// <summary>
///     Event emitted when a saga step is compensated.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.Sagas.Abstractions.SagaStepCompensated")]
[EventStorageName("MISSISSIPPI", "SAGAS", "SAGASTEPCOMPENSATED")]
public sealed record SagaStepCompensated
{
    /// <summary>
    ///     Gets the step index that was compensated.
    /// </summary>
    [Id(0)]
    public required int StepIndex { get; init; }

    /// <summary>
    ///     Gets the step name that was compensated.
    /// </summary>
    [Id(1)]
    public required string StepName { get; init; }
}
