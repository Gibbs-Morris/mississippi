using Mississippi.DomainModeling.Abstractions;

using MississippiSamples.Spring.Domain.Projections.MoneyTransferStatus;
using MississippiSamples.Spring.Domain.Projections.MoneyTransferStatus.Reducers;


namespace MississippiSamples.Spring.Domain.L0Tests.Projections.MoneyTransferStatus;

/// <summary>
///     Tests for <see cref="SagaStepFailedStatusReducer" />.
/// </summary>
public sealed class SagaStepFailedStatusReducerTests
{
    private readonly SagaStepFailedStatusReducer reducer = new();

    /// <summary>
    ///     Verifies null event throws.
    /// </summary>
    [Fact]
    public void ReduceWithNullEventThrows()
    {
        MoneyTransferStatusProjection initial = new();
        reducer.ShouldThrow<ArgumentNullException, SagaStepFailed, MoneyTransferStatusProjection>(
            initial,
            null!,
            "eventData");
    }

    /// <summary>
    ///     Verifies errors are captured when a step fails.
    /// </summary>
    [Fact]
    public void ReduceWithStepFailedCapturesError()
    {
        MoneyTransferStatusProjection initial = new()
        {
            RecoveryMode = SagaRecoveryMode.Automatic,
        };
        SagaStepFailed @event = new()
        {
            AttemptId = Guid.NewGuid(),
            StepIndex = 1,
            StepName = "Deposit",
            ErrorCode = "ERR",
            ErrorMessage = "failed",
            OperationKey = "op-1",
        };
        MoneyTransferStatusProjection result = reducer.Apply(initial, @event);
        result.ErrorCode.Should().Be("ERR");
        result.ErrorMessage.Should().Be("failed");
        result.PendingDirection.Should().Be(SagaExecutionDirection.Forward);
        result.PendingStepIndex.Should().Be(1);
        result.PendingStepName.Should().Be("Deposit");
        result.BlockedReason.Should().BeNull();
        result.ResumeDisposition.Should().Be(SagaResumeDisposition.AutomaticPending);
    }
}