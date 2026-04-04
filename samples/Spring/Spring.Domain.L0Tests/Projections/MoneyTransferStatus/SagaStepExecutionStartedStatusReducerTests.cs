using Mississippi.DomainModeling.Abstractions;

using MississippiSamples.Spring.Domain.Projections.MoneyTransferStatus;
using MississippiSamples.Spring.Domain.Projections.MoneyTransferStatus.Reducers;


namespace MississippiSamples.Spring.Domain.L0Tests.Projections.MoneyTransferStatus;

/// <summary>
///     Tests for <see cref="SagaStepExecutionStartedStatusReducer" />.
/// </summary>
public sealed class SagaStepExecutionStartedStatusReducerTests
{
    private readonly SagaStepExecutionStartedStatusReducer reducer = new();

    /// <summary>
    ///     Verifies null event throws.
    /// </summary>
    [Fact]
    public void ReduceWithNullEventThrows()
    {
        MoneyTransferStatusProjection initial = new();
        reducer.ShouldThrow<ArgumentNullException, SagaStepExecutionStarted, MoneyTransferStatusProjection>(
            initial,
            null!,
            "eventData");
    }

    /// <summary>
    ///     Verifies reminder-driven executions update recovery metadata.
    /// </summary>
    [Fact]
    public void ReduceWithReminderExecutionUpdatesRecoveryMetadata()
    {
        DateTimeOffset startedAt = new(2026, 4, 4, 9, 30, 0, TimeSpan.Zero);
        MoneyTransferStatusProjection initial = new()
        {
            RecoveryMode = SagaRecoveryMode.Automatic,
            ResumeDisposition = SagaResumeDisposition.Idle,
        };
        SagaStepExecutionStarted @event = new()
        {
            AttemptId = Guid.NewGuid(),
            Direction = SagaExecutionDirection.Forward,
            OperationKey = "reminder-op-1",
            Source = SagaResumeSource.Reminder,
            StartedAt = startedAt,
            StepIndex = 1,
            StepName = "Deposit",
        };
        MoneyTransferStatusProjection result = reducer.Apply(initial, @event);
        result.PendingDirection.Should().Be(SagaExecutionDirection.Forward);
        result.PendingStepIndex.Should().Be(1);
        result.PendingStepName.Should().Be("Deposit");
        result.BlockedReason.Should().BeNull();
        result.LastResumeSource.Should().Be(SagaResumeSource.Reminder);
        result.LastResumeAttemptedAt.Should().Be(startedAt);
        result.AutomaticAttemptCount.Should().Be(1);
        result.ResumeDisposition.Should().Be(SagaResumeDisposition.AutomaticPending);
    }
}