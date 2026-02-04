using Mississippi.EventSourcing.Sagas.Abstractions;

using Spring.Domain.Projections.MoneyTransferStatus;
using Spring.Domain.Projections.MoneyTransferStatus.Reducers;


namespace Spring.Domain.L0Tests.Projections.MoneyTransferStatus;

/// <summary>
///     Tests for <see cref="SagaCompensatedStatusReducer" />.
/// </summary>
public sealed class SagaCompensatedStatusReducerTests
{
    private readonly SagaCompensatedStatusReducer reducer = new();

    /// <summary>
    ///     Verifies phase updates to compensated.
    /// </summary>
    [Fact]
    public void ReduceWithCompensatedSetsPhase()
    {
        MoneyTransferStatusProjection initial = new()
        {
            Phase = SagaPhase.Compensating,
        };
        DateTimeOffset completedAt = new(2026, 2, 3, 12, 30, 0, TimeSpan.Zero);
        SagaCompensated @event = new()
        {
            CompletedAt = completedAt,
        };
        MoneyTransferStatusProjection result = reducer.Apply(initial, @event);
        result.Phase.Should().Be(SagaPhase.Compensated);
        result.CompletedAt.Should().Be(completedAt);
    }

    /// <summary>
    ///     Verifies null event throws.
    /// </summary>
    [Fact]
    public void ReduceWithNullEventThrows()
    {
        MoneyTransferStatusProjection initial = new();
        reducer.ShouldThrow<ArgumentNullException, SagaCompensated, MoneyTransferStatusProjection>(
            initial,
            null!,
            "eventData");
    }
}