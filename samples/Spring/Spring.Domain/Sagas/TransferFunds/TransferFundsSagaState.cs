using System;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;
using Mississippi.EventSourcing.Sagas.Abstractions;
using Mississippi.Inlet.Generators.Abstractions;

using Orleans;


namespace Spring.Domain.Sagas.TransferFunds;

/// <summary>
///     Saga state for the TransferFunds saga.
/// </summary>
/// <remarks>
///     <para>
///         This saga orchestrates the transfer of funds between two bank accounts.
///         It performs a two-phase transfer: first debiting the source account,
///         then crediting the destination account. If the credit fails, the saga
///         compensates by refunding the source account.
///     </para>
/// </remarks>
[BrookName("SPRING", "BANKING", "TRANSFER")]
[SnapshotStorageName("SPRING", "BANKING", "TRANSFERSTATE")]
[SagaOptions(CompensationStrategy = CompensationStrategy.Immediate)]
[GenerateSagaEndpoints(InputType = typeof(TransferFundsSagaInput))]
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
    [Id(11)]
    public string? CorrelationId { get; init; }

    /// <inheritdoc />
    [Id(14)]
    public int CurrentStepAttempt { get; init; } = 1;

    /// <summary>
    ///     Gets the account to deposit funds into.
    /// </summary>
    [Id(1)]
    public string DestinationAccountId { get; init; } = string.Empty;

    /// <summary>
    ///     Gets a value indicating whether the destination account has been credited.
    /// </summary>
    [Id(4)]
    public bool DestinationCredited { get; init; }

    /// <inheritdoc />
    [Id(13)]
    public int LastCompletedStepIndex { get; init; } = -1;

    /// <inheritdoc />
    [Id(12)]
    public SagaPhase Phase { get; init; } = SagaPhase.NotStarted;

    // ISagaState implementation

    /// <inheritdoc />
    [Id(10)]
    public Guid SagaId { get; init; }

    // Business data (populated from TransferInitiated event)

    /// <summary>
    ///     Gets the account to withdraw funds from.
    /// </summary>
    [Id(0)]
    public string SourceAccountId { get; init; } = string.Empty;

    /// <summary>
    ///     Gets a value indicating whether the source account has been debited.
    /// </summary>
    [Id(3)]
    public bool SourceDebited { get; init; }

    /// <inheritdoc />
    [Id(15)]
    public DateTimeOffset? StartedAt { get; init; }

    /// <inheritdoc />
    [Id(16)]
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