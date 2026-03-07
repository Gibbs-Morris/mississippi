using System;

using Mississippi.Brooks.Abstractions.Attributes;
using Mississippi.DomainModeling.Abstractions;
using Mississippi.Inlet.Generators.Abstractions;
using MississippiSamples.Spring.Domain.Aggregates.MoneyTransferSaga.Commands;

using Orleans;


namespace MississippiSamples.Spring.Domain.Aggregates.MoneyTransferSaga;

/// <summary>
///     Saga state for orchestrating money transfers between bank accounts.
/// </summary>
[BrookName("SPRING", "BANKING", "TRANSFER")]
[SnapshotStorageName("SPRING", "BANKING", "TRANSFERSTATE")]
[GenerateSagaEndpoints(
    InputType = typeof(StartMoneyTransferCommand),
    RoutePrefix = "money-transfer",
    FeatureKey = "moneyTransfer")]
[GenerateMcpSagaTools(
    Title = "Transfer Funds",
    Description = "Transfers funds between two Spring Bank accounts using the saga orchestrator.",
    ToolPrefix = "transfer_funds")]
[GenerateSerializer]
[Alias("MississippiSamples.Spring.Domain.Aggregates.MoneyTransferSaga.MoneyTransferSagaState")]
public sealed record MoneyTransferSagaState : ISagaState
{
    /// <summary>
    ///     Gets the correlation identifier for the saga instance.
    /// </summary>
    [Id(3)]
    public string? CorrelationId { get; init; }

    /// <summary>
    ///     Gets the captured input for the transfer.
    /// </summary>
    [Id(6)]
    public StartMoneyTransferCommand? Input { get; init; }

    /// <summary>
    ///     Gets the index of the last completed step.
    /// </summary>
    [Id(2)]
    public int LastCompletedStepIndex { get; init; } = -1;

    /// <summary>
    ///     Gets the current saga phase.
    /// </summary>
    [Id(1)]
    public SagaPhase Phase { get; init; }

    /// <summary>
    ///     Gets the saga identifier.
    /// </summary>
    [Id(0)]
    public Guid SagaId { get; init; }

    /// <summary>
    ///     Gets the timestamp when the saga started.
    /// </summary>
    [Id(4)]
    public DateTimeOffset? StartedAt { get; init; }

    /// <summary>
    ///     Gets the hash representing the ordered saga steps.
    /// </summary>
    [Id(5)]
    public string? StepHash { get; init; }
}