using System;

namespace Mississippi.EventSourcing.Sagas.Abstractions;

public interface ISagaState
{
    Guid SagaId { get; }
    SagaPhase Phase { get; }
    int LastCompletedStepIndex { get; }
    string? CorrelationId { get; }
    DateTimeOffset? StartedAt { get; }
    string? StepHash { get; }
}

public enum SagaPhase
{
    NotStarted,
    Running,
    Compensating,
    Completed,
    Compensated,
    Failed,
}
