using System;

using Mississippi.EventSourcing.Sagas.Abstractions;


namespace Mississippi.EventSourcing.Sagas.L0Tests.Helpers;

/// <summary>
///     Saga with Manual compensation strategy for testing.
/// </summary>
[SagaOptions(CompensationStrategy = CompensationStrategy.Manual)]
internal sealed record ManualStrategySaga : ISagaState
{
    /// <inheritdoc />
    public string? CorrelationId { get; init; }

    /// <inheritdoc />
    public int CurrentStepAttempt { get; init; } = 1;

    /// <inheritdoc />
    public int LastCompletedStepIndex { get; init; }

    /// <inheritdoc />
    public SagaPhase Phase { get; init; } = SagaPhase.Running;

    /// <inheritdoc />
    public Guid SagaId { get; init; }

    /// <inheritdoc />
    public DateTimeOffset? StartedAt { get; init; }

    /// <inheritdoc />
    public string? StepHash { get; init; }

    /// <inheritdoc />
    public ISagaState ApplyPhase(
        SagaPhase phase
    ) =>
        this with
        {
            Phase = phase,
        };

    /// <inheritdoc />
    public ISagaState ApplySagaStarted(
        Guid sagaId,
        string? correlationId,
        string? stepHash,
        DateTimeOffset startedAt
    ) =>
        this with
        {
            SagaId = sagaId,
            CorrelationId = correlationId,
            StepHash = stepHash,
            StartedAt = startedAt,
            Phase = SagaPhase.Running,
            LastCompletedStepIndex = -1,
            CurrentStepAttempt = 1,
        };

    /// <inheritdoc />
    public ISagaState ApplyStepProgress(
        int lastCompletedStepIndex,
        int currentStepAttempt
    ) =>
        this with
        {
            LastCompletedStepIndex = lastCompletedStepIndex,
            CurrentStepAttempt = currentStepAttempt,
        };
}