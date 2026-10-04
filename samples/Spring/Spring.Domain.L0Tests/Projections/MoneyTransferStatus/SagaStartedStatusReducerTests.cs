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
        };
        MoneyTransferStatusProjection result = reducer.Apply(initial, @event);
        Assert.Equal(SagaPhase.Running, result.Phase);
        Assert.Equal(startedAt, result.StartedAt);
        Assert.Equal(-1, result.LastCompletedStepIndex);
        Assert.Null(result.ErrorCode);
        Assert.Null(result.ErrorMessage);
        Assert.Null(result.CompletedAt);
    }
}