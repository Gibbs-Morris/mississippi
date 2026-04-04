using Mississippi.DomainModeling.Abstractions;

using MississippiSamples.Spring.Domain.Projections.MoneyTransferStatus;
using MississippiSamples.Spring.Domain.Projections.MoneyTransferStatus.Reducers;


namespace MississippiSamples.Spring.Domain.L0Tests.Projections.MoneyTransferStatus;

/// <summary>
///     Tests for <see cref="SagaCompensatingStatusReducer" />.
/// </summary>
public sealed class SagaCompensatingStatusReducerTests
{
    private readonly SagaCompensatingStatusReducer reducer = new();

    /// <summary>
    ///     Verifies phase updates to compensating.
    /// </summary>
    [Fact]
    public void ReduceWithCompensatingSetsPhase()
    {
        MoneyTransferStatusProjection initial = new()
        {
            Phase = SagaPhase.Running,
            RecoveryMode = SagaRecoveryMode.Automatic,
            BlockedReason = "Needs manual resume.",
            ResumeDisposition = SagaResumeDisposition.ManualInterventionRequired,
        };
        SagaCompensating @event = new()
        {
            FromStepIndex = 0,
        };
        MoneyTransferStatusProjection result = reducer.Apply(initial, @event);
        result.Phase.Should().Be(SagaPhase.Compensating);
        result.PendingDirection.Should().Be(SagaExecutionDirection.Compensation);
        result.PendingStepIndex.Should().Be(0);
        result.PendingStepName.Should().BeNull();
        result.BlockedReason.Should().BeNull();
        result.ResumeDisposition.Should().Be(SagaResumeDisposition.AutomaticPending);
    }

    /// <summary>
    ///     Verifies null event throws.
    /// </summary>
    [Fact]
    public void ReduceWithNullEventThrows()
    {
        MoneyTransferStatusProjection initial = new();
        reducer.ShouldThrow<ArgumentNullException, SagaCompensating, MoneyTransferStatusProjection>(
            initial,
            null!,
            "eventData");
    }
}