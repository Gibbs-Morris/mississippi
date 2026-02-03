using System;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;
using Mississippi.EventSourcing.Sagas.Abstractions;
using Mississippi.Inlet.Abstractions;
using Mississippi.Inlet.Generators.Abstractions;

using Orleans;


namespace Spring.Domain.Projections.MoneyTransferStatus;

/// <summary>
///     Read-optimized projection for tracking money transfer saga status.
/// </summary>
[ProjectionPath("money-transfer-status")]
[BrookName("SPRING", "BANKING", "TRANSFER")]
[SnapshotStorageName("SPRING", "BANKING", "TRANSFERSTATUS")]
[GenerateProjectionEndpoints]
[GenerateSerializer]
[GenerateSagaStatusReducers]
[Alias("Spring.Domain.Projections.MoneyTransferStatus.MoneyTransferStatusProjection")]
public sealed record MoneyTransferStatusProjection
{
    /// <summary>
    ///     Gets the timestamp when the saga completed or failed.
    /// </summary>
    [Id(5)]
    public DateTimeOffset? CompletedAt { get; init; }

    /// <summary>
    ///     Gets the error code for the last failure, if any.
    /// </summary>
    [Id(2)]
    public string? ErrorCode { get; init; }

    /// <summary>
    ///     Gets the error message for the last failure, if any.
    /// </summary>
    [Id(3)]
    public string? ErrorMessage { get; init; }

    /// <summary>
    ///     Gets the index of the last completed step.
    /// </summary>
    [Id(1)]
    public int LastCompletedStepIndex { get; init; } = -1;

    /// <summary>
    ///     Gets the current saga phase.
    /// </summary>
    [Id(0)]
    public SagaPhase Phase { get; init; } = SagaPhase.NotStarted;

    /// <summary>
    ///     Gets the timestamp when the saga started.
    /// </summary>
    [Id(4)]
    public DateTimeOffset? StartedAt { get; init; }
}