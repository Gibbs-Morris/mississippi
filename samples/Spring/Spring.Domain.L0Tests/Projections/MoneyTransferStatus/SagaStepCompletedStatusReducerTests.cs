using Mississippi.EventSourcing.Sagas.Abstractions;

using Spring.Domain.Projections.MoneyTransferStatus;
using Spring.Domain.Projections.MoneyTransferStatus.Reducers;


namespace Spring.Domain.L0Tests.Projections.MoneyTransferStatus;

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
        MoneyTransferStatusProjection initial = new();
        SagaStepCompleted @event = new()
        {
            StepIndex = 1,
            StepName = "Deposit",
            CompletedAt = new(2026, 2, 3, 10, 0, 0, TimeSpan.Zero),
        };
        MoneyTransferStatusProjection result = reducer.Apply(initial, @event);
        result.LastCompletedStepIndex.Should().Be(1);
    }
}