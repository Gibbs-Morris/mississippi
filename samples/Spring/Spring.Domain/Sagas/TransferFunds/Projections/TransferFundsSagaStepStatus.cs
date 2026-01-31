using System;

using Orleans;


namespace Spring.Domain.Sagas.TransferFunds.Projections;

/// <summary>
///     Represents the status of a TransferFunds saga step.
/// </summary>
[GenerateSerializer]
[Alias("Spring.Domain.Sagas.TransferFunds.Projections.TransferFundsSagaStepStatus")]
public sealed record TransferFundsSagaStepStatus
{
    /// <summary>
    ///     Gets the name of the step.
    /// </summary>
    [Id(0)]
    public string StepName { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the execution order of the step.
    /// </summary>
    [Id(1)]
    public int StepOrder { get; init; }

    /// <summary>
    ///     Gets when the step reached its current outcome.
    /// </summary>
    [Id(2)]
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    ///     Gets the outcome of the step.
    /// </summary>
    [Id(3)]
    public string Outcome { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the error message if the step failed.
    /// </summary>
    [Id(4)]
    public string? ErrorMessage { get; init; }
}
