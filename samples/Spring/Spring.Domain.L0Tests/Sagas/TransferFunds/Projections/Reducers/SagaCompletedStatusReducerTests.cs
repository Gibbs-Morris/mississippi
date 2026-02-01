using Mississippi.EventSourcing.Sagas.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions.Events;

using Spring.Domain.Sagas.TransferFunds.Projections;
using Spring.Domain.Sagas.TransferFunds.Projections.Reducers;


namespace Spring.Domain.L0Tests.Sagas.TransferFunds.Projections.Reducers;

/// <summary>
///     Tests for <see cref="SagaCompletedStatusReducer" />.
/// </summary>
public sealed class SagaCompletedStatusReducerTests
{
    private readonly SagaCompletedStatusReducer reducer = new();

    /// <summary>
    ///     Verifies that reducer throws when event is null.
    /// </summary>
    [Fact]
    public void ReduceWithNullEventThrowsArgumentNullException()
    {
        // Arrange
        TransferFundsSagaStatusProjection initial = new();

        // Act & Assert
        reducer.ShouldThrow<ArgumentNullException, SagaCompletedEvent, TransferFundsSagaStatusProjection>(
            initial,
            null!,
            "eventData");
    }

    /// <summary>
    ///     Verifies that applying SagaCompleted marks the saga as completed.
    /// </summary>
    [Fact]
    public void ReduceWithSagaCompletedMarksCompleted()
    {
        // Arrange
        TransferFundsSagaStatusProjection initial = new()
        {
            Phase = SagaPhase.Running.ToString(),
            CurrentStep = null,
        };
        DateTimeOffset completedAt = new(2024, 2, 1, 0, 3, 0, TimeSpan.Zero);
        SagaCompletedEvent evt = new(completedAt);

        // Act
        TransferFundsSagaStatusProjection result = reducer.Apply(initial, evt);

        // Assert
        result.Phase.Should().Be(SagaPhase.Completed.ToString());
        result.CompletedAt.Should().Be(completedAt);
        result.CurrentStep.Should().BeNull();
    }
}