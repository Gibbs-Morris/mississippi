using Mississippi.EventSourcing.Sagas.Abstractions;

using Spring.Domain.Projections.MoneyTransferStatus;
using Spring.Domain.Projections.MoneyTransferStatus.Reducers;


namespace Spring.Domain.L0Tests.Projections.MoneyTransferStatus;

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
        };
        DateTimeOffset completedAt = new(2026, 2, 3, 11, 15, 0, TimeSpan.Zero);
        SagaCompleted @event = new()
        {
            CompletedAt = completedAt,
        };
        MoneyTransferStatusProjection result = reducer.Apply(initial, @event);
        result.Phase.Should().Be(SagaPhase.Completed);
        result.CompletedAt.Should().Be(completedAt);
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