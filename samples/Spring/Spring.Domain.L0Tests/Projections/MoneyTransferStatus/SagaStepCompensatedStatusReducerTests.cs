using Mississippi.DomainModeling.Abstractions;

using MississippiSamples.Spring.Domain.Projections.MoneyTransferStatus;
using MississippiSamples.Spring.Domain.Projections.MoneyTransferStatus.Reducers;


namespace MississippiSamples.Spring.Domain.L0Tests.Projections.MoneyTransferStatus;

/// <summary>
///     Tests for <see cref="SagaStepCompensatedStatusReducer" />.
/// </summary>
public sealed class SagaStepCompensatedStatusReducerTests
{
    private readonly SagaStepCompensatedStatusReducer reducer = new();

    /// <summary>
    ///     Verifies null event throws.
    /// </summary>
    [Fact]
    public void ReduceWithNullEventThrows()
    {
        MoneyTransferStatusProjection initial = new();
        reducer.ShouldThrow<ArgumentNullException, SagaStepCompensated, MoneyTransferStatusProjection>(
            initial,
            null!,
            "eventData");
    }

    /// <summary>
    ///     Verifies compensated steps move pending work backward through compensation.
    /// </summary>
    [Fact]
    public void ReduceWithStepCompensatedMovesCompensationCursor()
    {
        MoneyTransferStatusProjection initial = new()
        {
            RecoveryMode = SagaRecoveryMode.Automatic,
            PendingDirection = SagaExecutionDirection.Compensation,
            PendingStepIndex = 1,
            PendingStepName = "Deposit",
            ResumeDisposition = SagaResumeDisposition.ManualInterventionRequired,
            BlockedReason = "Manual replay required.",
        };
        SagaStepCompensated @event = new()
        {
            AttemptId = Guid.NewGuid(),
            OperationKey = "comp-op-1",
            StepIndex = 1,
            StepName = "Deposit",
        };
        MoneyTransferStatusProjection result = reducer.Apply(initial, @event);
        result.PendingDirection.Should().Be(SagaExecutionDirection.Compensation);
        result.PendingStepIndex.Should().Be(0);
        result.PendingStepName.Should().BeNull();
        result.BlockedReason.Should().BeNull();
        result.ResumeDisposition.Should().Be(SagaResumeDisposition.AutomaticPending);
    }
}