using System;


namespace Mississippi.EventSourcing.Sagas.Abstractions.Projections;

/// <summary>
///     Represents a record of a saga step's execution.
/// </summary>
/// <param name="StepName">The name of the step.</param>
/// <param name="StepOrder">The execution order of the step.</param>
/// <param name="Timestamp">When the step reached its current outcome.</param>
/// <param name="Outcome">The outcome of the step.</param>
/// <param name="ErrorMessage">An optional error message if the step failed.</param>
public sealed record SagaStepRecord(
    string StepName,
    int StepOrder,
    DateTimeOffset Timestamp,
    StepOutcome Outcome,
    string? ErrorMessage = null
);
