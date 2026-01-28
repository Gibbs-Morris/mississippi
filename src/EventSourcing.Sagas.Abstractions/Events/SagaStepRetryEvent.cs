using System;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;


namespace Mississippi.EventSourcing.Sagas.Abstractions.Events;

/// <summary>
///     Emitted when a saga step is being retried after a failure.
/// </summary>
/// <param name="StepName">The name of the step being retried.</param>
/// <param name="StepOrder">The execution order of the step.</param>
/// <param name="AttemptNumber">The attempt number (1-based; first retry is attempt 2).</param>
/// <param name="MaxAttempts">The maximum number of attempts allowed.</param>
/// <param name="PreviousErrorCode">The error code from the previous failed attempt.</param>
/// <param name="PreviousErrorMessage">The error message from the previous failed attempt.</param>
/// <param name="Timestamp">When the retry was initiated.</param>
/// <remarks>
///     <para>
///         This event is emitted when using <see cref="CompensationStrategy.RetryThenCompensate" />
///         before re-executing a step that failed on a previous attempt.
///     </para>
/// </remarks>
[EventStorageName("MISSISSIPPI", "SAGAS", "SAGASTEPRETRY")]
public sealed record SagaStepRetryEvent(
    string StepName,
    int StepOrder,
    int AttemptNumber,
    int MaxAttempts,
    string PreviousErrorCode,
    string? PreviousErrorMessage,
    DateTimeOffset Timestamp
);