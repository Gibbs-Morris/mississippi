using Mississippi.Brooks.Abstractions.Attributes;

using Orleans;


namespace Mississippi.DomainModeling.Abstractions;

/// <summary>
///     Event emitted when a saga step is compensated.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.DomainModeling.Abstractions.SagaStepCompensated")]
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