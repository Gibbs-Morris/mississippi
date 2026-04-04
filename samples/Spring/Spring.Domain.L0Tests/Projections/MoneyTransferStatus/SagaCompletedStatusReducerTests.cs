using Mississippi.DomainModeling.Abstractions;

using MississippiSamples.Spring.Domain.Projections.MoneyTransferStatus;
using MississippiSamples.Spring.Domain.Projections.MoneyTransferStatus.Reducers;


namespace MississippiSamples.Spring.Domain.L0Tests.Projections.MoneyTransferStatus;

/// <summary>
///     Tests for <see cref="SagaCompletedStatusReducer" />.
/// </summary>
public sealed class SagaCompletedStatusReducerTests
{
    private readonly SagaCompletedStatusReducer reducer = new();

    /// <summary>
    ///     Verifies phase updates to completed.
    /// </summary>
    [Fact]
    public void ReduceWithCompletedSetsPhase()
    {
        MoneyTransferStatusProjection initial = new()
        {
            Phase = SagaPhase.Running,
            PendingDirection = SagaExecutionDirection.Forward,
            PendingStepIndex = 2,
            PendingStepName = "Complete",
            BlockedReason = "Needs manual resume.",
            ResumeDisposition = SagaResumeDisposition.ManualInterventionRequired,
        };
        DateTimeOffset completedAt = new(2026, 2, 3, 11, 15, 0, TimeSpan.Zero);
        SagaCompleted @event = new()
        {
            CompletedAt = completedAt,
        };
        MoneyTransferStatusProjection result = reducer.Apply(initial, @event);
        result.Phase.Should().Be(SagaPhase.Completed);
        result.CompletedAt.Should().Be(completedAt);
        result.PendingDirection.Should().BeNull();
        result.PendingStepIndex.Should().BeNull();
        result.PendingStepName.Should().BeNull();
        result.BlockedReason.Should().BeNull();
        result.ResumeDisposition.Should().Be(SagaResumeDisposition.Terminal);
    }

    /// <summary>
    ///     Verifies null event throws.
    /// </summary>
    [Fact]
    public void ReduceWithNullEventThrows()
    {
        MoneyTransferStatusProjection initial = new();
        reducer.ShouldThrow<ArgumentNullException, SagaCompleted, MoneyTransferStatusProjection>(
            initial,
            null!,
            "eventData");
    }
}