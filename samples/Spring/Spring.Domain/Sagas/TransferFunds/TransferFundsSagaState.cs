using System;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;
using Mississippi.EventSourcing.Sagas.Abstractions;
using Mississippi.Inlet.Generators.Abstractions;

using Orleans;


namespace Spring.Domain.Sagas.TransferFunds;

/// <summary>
///     Saga state for a money transfer between two bank accounts.
/// </summary>
/// <remarks>
///     <para>
///         The TransferFunds saga orchestrates a two-phase transfer:
///         <list type="number">
///             <item>Debit the source account</item>
///             <item>Credit the destination account</item>
///         </list>
///     </para>
///     <para>
///         If the credit step fails, the saga compensates by refunding the source account.
///     </para>
///     <para>
///         Implements <see cref="ISagaState" /> to enable proper saga identity propagation,
///         idempotency guards, and step hash validation in the saga infrastructure.
///     </para>
/// </remarks>
[SagaOptions(
    CompensationStrategy = CompensationStrategy.Immediate,
    DefaultStepTimeout = "00:00:30",
    TimeoutBehavior = TimeoutBehavior.FailAndCompensate,
    MaxRetries = 3)]
[BrookName("SPRING", "BANKING", "TRANSFER")]
[SnapshotStorageName("SPRING", "BANKING", "TRANSFERSTATE")]
[GenerateSagaEndpoints]
[GenerateSerializer]
[Alias("Spring.Domain.Sagas.TransferFunds.TransferFundsSagaState")]
public sealed record TransferFundsSagaState
    : ISagaDefinition,
      ISagaState
{
    /// <inheritdoc />
    public static string SagaName => "TransferFunds";

    /// <summary>
    ///     Gets the amount to transfer.
    /// </summary>
    [Id(2)]
    public decimal Amount { get; init; }

    /// <inheritdoc />
    [Id(9)]
    public string? CorrelationId { get; init; }

    /// <inheritdoc />
    [Id(14)]
    public int CurrentStepAttempt { get; init; } = 1;

    /// <summary>
    ///     Gets the destination account ID.
    /// </summary>
    [Id(1)]
    public required Guid DestinationAccountId { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the destination account has been credited.
    /// </summary>
    [Id(4)]
    public bool DestinationCredited { get; init; }

    /// <summary>
    ///     Gets the reason for failure if the transfer failed.
    /// </summary>
    [Id(7)]
    public string? FailureReason { get; init; }

    /// <summary>
    ///     Gets the transfer initiation timestamp.
    /// </summary>
    [Id(6)]
    public DateTimeOffset InitiatedAt { get; init; }

    /// <inheritdoc />
    [Id(12)]
    public int LastCompletedStepIndex { get; init; } = -1;

    /// <inheritdoc />
    [Id(11)]
    public SagaPhase Phase { get; init; }

    // ISagaState implementation for saga infrastructure integration

    /// <inheritdoc />
    [Id(8)]
    public Guid SagaId { get; init; }

    /// <summary>
    ///     Gets the source account ID.
    /// </summary>
    [Id(0)]
    public required Guid SourceAccountId { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the source account has been debited.
    /// </summary>
    [Id(3)]
    public bool SourceDebited { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the source account has been refunded (compensation).
    /// </summary>
    [Id(5)]
    public bool SourceRefunded { get; init; }

    /// <inheritdoc />
    [Id(13)]
    public DateTimeOffset? StartedAt { get; init; }

    /// <inheritdoc />
    [Id(10)]
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