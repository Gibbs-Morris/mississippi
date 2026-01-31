using System;
using System.Collections.Immutable;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;
using Mississippi.Inlet.Abstractions;
using Mississippi.Inlet.Generators.Abstractions;

using Orleans;


namespace Spring.Domain.Sagas.TransferFunds.Projections;

/// <summary>
///     Real-time status projection for the TransferFunds saga.
/// </summary>
/// <remarks>
///     <para>
///         This projection provides visibility into the current status of a
///         TransferFunds saga instance. It subscribes to the saga's event stream
///         and enables real-time status updates via SignalR.
///     </para>
///     <para>
///         The projection tracks:
///         <list type="bullet">
///             <item>Current phase (NotStarted, Running, Completed, Failed, Compensating)</item>
///             <item>Step progress (current step, completed steps)</item>
///             <item>Timing information (start time, completion time)</item>
///             <item>Failure details when applicable</item>
///         </list>
///     </para>
/// </remarks>
[ProjectionPath("transfer-saga-status")]
[BrookName("SPRING", "BANKING", "TRANSFER")]
[SnapshotStorageName("SPRING", "BANKING", "TRANSFERSTATUS")]
[GenerateProjectionEndpoints]
[GenerateSerializer]
[Alias("Spring.Domain.Sagas.TransferFunds.Projections.TransferFundsSagaStatusProjection")]
public sealed record TransferFundsSagaStatusProjection
{
    /// <summary>
    ///     Gets the unique identifier of the saga instance.
    /// </summary>
    [Id(0)]
    public string SagaId { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the type name of the saga.
    /// </summary>
    [Id(1)]
    public string SagaType { get; init; } = nameof(TransferFundsSagaState);

    /// <summary>
    ///     Gets the current phase of the saga.
    /// </summary>
    [Id(2)]
    public string Phase { get; init; } = "NotStarted";

    /// <summary>
    ///     Gets when the saga started, if it has started.
    /// </summary>
    [Id(3)]
    public DateTimeOffset? StartedAt { get; init; }

    /// <summary>
    ///     Gets when the saga completed, if it has completed.
    /// </summary>
    [Id(4)]
    public DateTimeOffset? CompletedAt { get; init; }

    /// <summary>
    ///     Gets the currently executing step, if any.
    /// </summary>
    [Id(5)]
    public TransferFundsSagaStepStatus? CurrentStep { get; init; }

    /// <summary>
    ///     Gets the list of steps that have completed successfully.
    /// </summary>
    [Id(6)]
    public ImmutableArray<TransferFundsSagaStepStatus> CompletedSteps { get; init; } =
        ImmutableArray<TransferFundsSagaStepStatus>.Empty;

    /// <summary>
    ///     Gets the list of steps that have failed.
    /// </summary>
    [Id(7)]
    public ImmutableArray<TransferFundsSagaStepStatus> FailedSteps { get; init; } =
        ImmutableArray<TransferFundsSagaStepStatus>.Empty;

    /// <summary>
    ///     Gets the reason for saga failure, if the saga failed.
    /// </summary>
    [Id(8)]
    public string? FailureReason { get; init; }

    /// <summary>
    ///     Gets the total number of steps in the saga.
    /// </summary>
    [Id(9)]
    public int TotalSteps { get; init; } = 2; // DebitSourceAccount, CreditDestinationAccount
}
