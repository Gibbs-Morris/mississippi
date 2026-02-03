using Mississippi.EventSourcing.Sagas.Abstractions;

using Spring.Domain.Projections.MoneyTransferStatus;
using Spring.Domain.Projections.MoneyTransferStatus.Reducers;


namespace Spring.Domain.L0Tests.Projections.MoneyTransferStatus;

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
        };
        MoneyTransferStatusProjection result = reducer.Apply(initial, @event);
        result.Phase.Should().Be(SagaPhase.Running);
        result.StartedAt.Should().Be(startedAt);
        result.LastCompletedStepIndex.Should().Be(-1);
        result.ErrorCode.Should().BeNull();
        result.ErrorMessage.Should().BeNull();
        result.CompletedAt.Should().BeNull();
    }
}