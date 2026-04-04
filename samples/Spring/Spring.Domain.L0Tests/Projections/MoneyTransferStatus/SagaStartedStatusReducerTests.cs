using Mississippi.DomainModeling.Abstractions;

using MississippiSamples.Spring.Domain.Projections.MoneyTransferStatus;
using MississippiSamples.Spring.Domain.Projections.MoneyTransferStatus.Reducers;


namespace MississippiSamples.Spring.Domain.L0Tests.Projections.MoneyTransferStatus;

/// <summary>
///     Tests for <see cref="SagaStartedStatusReducer" />.
/// </summary>
public sealed class SagaStartedStatusReducerTests
{
    private readonly SagaStartedStatusReducer reducer = new();

    /// <summary>
    ///     Verifies null event throws.
    /// </summary>
    [Fact]
    public void ReduceWithNullEventThrows()
    {
        MoneyTransferStatusProjection initial = new();
        reducer.ShouldThrow<ArgumentNullException, SagaStartedEvent, MoneyTransferStatusProjection>(
            initial,
            null!,
            "eventData");
    }

    /// <summary>
    ///     Verifies saga started initializes status fields.
    /// </summary>
    [Fact]
    public void ReduceWithSagaStartedSetsRunningState()
    {
        MoneyTransferStatusProjection initial = new()
        {
            Phase = SagaPhase.NotStarted,
            LastCompletedStepIndex = 2,
        };
        DateTimeOffset startedAt = new(2026, 2, 3, 9, 0, 0, TimeSpan.Zero);
        SagaStartedEvent @event = new()
        {
            SagaId = Guid.NewGuid(),
            StepHash = "HASH",
            StartedAt = startedAt,
            CorrelationId = "corr-1",
            RecoveryMode = SagaRecoveryMode.Automatic,
        };
        MoneyTransferStatusProjection result = reducer.Apply(initial, @event);
        result.Phase.Should().Be(SagaPhase.Running);
        result.StartedAt.Should().Be(startedAt);
        result.LastCompletedStepIndex.Should().Be(-1);
        result.ErrorCode.Should().BeNull();
        result.ErrorMessage.Should().BeNull();
        result.CompletedAt.Should().BeNull();
        result.RecoveryMode.Should().Be(SagaRecoveryMode.Automatic);
        result.PendingDirection.Should().Be(SagaExecutionDirection.Forward);
        result.PendingStepIndex.Should().Be(0);
        result.PendingStepName.Should().BeNull();
        result.BlockedReason.Should().BeNull();
        result.LastResumeSource.Should().BeNull();
        result.LastResumeAttemptedAt.Should().BeNull();
        result.AutomaticAttemptCount.Should().Be(0);
        result.ResumeDisposition.Should().Be(SagaResumeDisposition.AutomaticPending);
    }
}