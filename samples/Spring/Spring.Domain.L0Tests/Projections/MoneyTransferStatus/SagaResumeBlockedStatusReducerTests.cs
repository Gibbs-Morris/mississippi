using Mississippi.DomainModeling.Abstractions;

using MississippiSamples.Spring.Domain.Projections.MoneyTransferStatus;
using MississippiSamples.Spring.Domain.Projections.MoneyTransferStatus.Reducers;


namespace MississippiSamples.Spring.Domain.L0Tests.Projections.MoneyTransferStatus;

/// <summary>
///     Tests for <see cref="SagaResumeBlockedStatusReducer" />.
/// </summary>
public sealed class SagaResumeBlockedStatusReducerTests
{
    private readonly SagaResumeBlockedStatusReducer reducer = new();

    /// <summary>
    ///     Verifies null event throws.
    /// </summary>
    [Fact]
    public void ReduceWithNullEventThrows()
    {
        MoneyTransferStatusProjection initial = new();
        reducer.ShouldThrow<ArgumentNullException, SagaResumeBlocked, MoneyTransferStatusProjection>(
            initial,
            null!,
            "eventData");
    }

    /// <summary>
    ///     Verifies blocked resume events surface operator metadata.
    /// </summary>
    [Fact]
    public void ReduceWithResumeBlockedCapturesOperatorRecoveryState()
    {
        DateTimeOffset blockedAt = new(2026, 4, 4, 10, 15, 0, TimeSpan.Zero);
        MoneyTransferStatusProjection initial = new()
        {
            RecoveryMode = SagaRecoveryMode.Automatic,
            ResumeDisposition = SagaResumeDisposition.AutomaticPending,
        };
        SagaResumeBlocked @event = new()
        {
            BlockedAt = blockedAt,
            BlockedReason = "Manual replay required for non-idempotent money movement.",
            Direction = SagaExecutionDirection.Forward,
            Source = SagaResumeSource.Reminder,
            StepIndex = 0,
            StepName = "Withdraw",
        };
        MoneyTransferStatusProjection result = reducer.Apply(initial, @event);
        result.PendingDirection.Should().Be(SagaExecutionDirection.Forward);
        result.PendingStepIndex.Should().Be(0);
        result.PendingStepName.Should().Be("Withdraw");
        result.BlockedReason.Should().Be(@event.BlockedReason);
        result.LastResumeSource.Should().Be(SagaResumeSource.Reminder);
        result.LastResumeAttemptedAt.Should().Be(blockedAt);
        result.AutomaticAttemptCount.Should().Be(1);
        result.ResumeDisposition.Should().Be(SagaResumeDisposition.ManualInterventionRequired);
    }
}