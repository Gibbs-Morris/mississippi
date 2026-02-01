using System;
using System.Collections.Immutable;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;


namespace Mississippi.EventSourcing.Sagas.Abstractions.Projections;

/// <summary>
///     A projection providing visibility into the current status of a saga.
///     Auto-registered by the saga infrastructure.
/// </summary>
/// <remarks>
///     <para>
///         This projection is derived from saga lifecycle events and provides
///         a read-model for monitoring and management UIs.
///     </para>
/// </remarks>
[SnapshotStorageName("MISSISSIPPI", "SAGAS", "SAGASTATUSPROJECTION", 1)]
public sealed record SagaStatusProjection
{
    /// <summary>
    ///     Gets when the saga completed, if it has completed.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; init; }

    /// <summary>
    ///     Gets the list of steps that have completed successfully.
    /// </summary>
    public ImmutableList<SagaStepRecord> CompletedSteps { get; init; } = [];

    /// <summary>
    ///     Gets the currently executing step, if any.
    /// </summary>
    public SagaStepRecord? CurrentStep { get; init; }

    /// <summary>
    ///     Gets the list of steps that have failed.
    /// </summary>
    public ImmutableList<SagaStepRecord> FailedSteps { get; init; } = [];

    /// <summary>
    ///     Gets the reason for saga failure, if the saga failed.
    /// </summary>
    public string? FailureReason { get; init; }

    /// <summary>
    ///     Gets the current phase of the saga.
    /// </summary>
    public SagaPhase Phase { get; init; } = SagaPhase.NotStarted;

    /// <summary>
    ///     Gets the unique identifier of the saga instance.
    /// </summary>
    public string SagaId { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the type name of the saga state.
    /// </summary>
    public string SagaType { get; init; } = string.Empty;

    /// <summary>
    ///     Gets when the saga started, if it has started.
    /// </summary>
    public DateTimeOffset? StartedAt { get; init; }
}