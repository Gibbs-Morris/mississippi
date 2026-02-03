using System;

using Mississippi.EventSourcing.Sagas.Abstractions;

namespace Mississippi.EventSourcing.Sagas.L0Tests;

/// <summary>
///     Test saga state used by saga runtime tests.
/// </summary>
internal sealed record TestSagaState : ISagaState
{
    /// <summary>
    ///     Gets the saga identifier.
    /// </summary>
    public Guid SagaId { get; init; }

    /// <summary>
    ///     Gets the saga phase.
    /// </summary>
    public SagaPhase Phase { get; init; }

    /// <summary>
    ///     Gets the index of the last completed step.
    /// </summary>
    public int LastCompletedStepIndex { get; init; } = -1;

    /// <summary>
    ///     Gets the correlation identifier.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    ///     Gets the timestamp when the saga started.
    /// </summary>
    public DateTimeOffset? StartedAt { get; init; }

    /// <summary>
    ///     Gets the step hash.
    /// </summary>
    public string? StepHash { get; init; }

    /// <summary>
    ///     Gets the test name for verification.
    /// </summary>
    public string Name { get; init; } = string.Empty;
}
