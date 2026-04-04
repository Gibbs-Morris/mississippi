using Mississippi.DomainModeling.Abstractions;

using MississippiSamples.Spring.Domain.Projections.MoneyTransferStatus;
using MississippiSamples.Spring.Domain.Projections.MoneyTransferStatus.Reducers;


namespace MississippiSamples.Spring.Domain.L0Tests.Projections.MoneyTransferStatus;

/// <summary>
///     Tests for <see cref="SagaStepCompletedStatusReducer" />.
/// </summary>
public sealed class SagaStepCompletedStatusReducerTests
{
    private readonly SagaStepCompletedStatusReducer reducer = new();

    /// <summary>
    ///     Verifies null event throws.
    /// </summary>
    [Fact]
    public void ReduceWithNullEventThrows()
    {
        MoneyTransferStatusProjection initial = new();
        reducer.ShouldThrow<ArgumentNullException, SagaStepCompleted, MoneyTransferStatusProjection>(
            initial,
            null!,
            "eventData");
    }

    /// <summary>
    ///     Verifies last completed step index updates.
    /// </summary>
    [Fact]
    public void ReduceWithStepCompletedUpdatesIndex()
    {
        MoneyTransferStatusProjection initial = new()
        {
            RecoveryMode = SagaRecoveryMode.Automatic,
            PendingDirection = SagaExecutionDirection.Forward,
            PendingStepIndex = 1,
            PendingStepName = "Withdraw",
            BlockedReason = "Needs manual resume.",
            ResumeDisposition = SagaResumeDisposition.ManualInterventionRequired,
        };
        SagaStepCompleted @event = new()
        {
            AttemptId = Guid.NewGuid(),
            OperationKey = "op-1",
            StepIndex = 1,
            StepName = "Deposit",
            CompletedAt = new(2026, 2, 3, 10, 0, 0, TimeSpan.Zero),
        };
        MoneyTransferStatusProjection result = reducer.Apply(initial, @event);
        result.LastCompletedStepIndex.Should().Be(1);
        result.PendingDirection.Should().Be(SagaExecutionDirection.Forward);
        result.PendingStepIndex.Should().Be(2);
        result.PendingStepName.Should().BeNull();
        result.BlockedReason.Should().BeNull();
        result.ResumeDisposition.Should().Be(SagaResumeDisposition.AutomaticPending);
    }
}