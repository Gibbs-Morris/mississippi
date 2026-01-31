using Mississippi.EventSourcing.Sagas.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions.Events;

using Spring.Domain.Sagas.TransferFunds.Projections;
using Spring.Domain.Sagas.TransferFunds.Projections.Reducers;


namespace Spring.Domain.L0Tests.Sagas.TransferFunds.Projections.Reducers;

/// <summary>
///     Tests for <see cref="SagaStartedStatusReducer" />.
/// </summary>
public sealed class SagaStartedStatusReducerTests
{
    private readonly SagaStartedStatusReducer reducer = new();

    /// <summary>
    ///     Verifies that applying SagaStarted initializes the status projection.
    /// </summary>
    [Fact]
    public void ReduceWithSagaStartedInitializesStatus()
    {
        // Arrange
        TransferFundsSagaStatusProjection initial = new();
        SagaStartedEvent evt = new(
            "saga-123",
            "TransferFundsSagaState",
            "step-hash",
            "correlation-1",
            new DateTimeOffset(2024, 2, 1, 0, 0, 0, TimeSpan.Zero));

        // Act
        TransferFundsSagaStatusProjection result = reducer.Apply(initial, evt);

        // Assert
        result.SagaId.Should().Be("saga-123");
        result.SagaType.Should().Be("TransferFundsSagaState");
        result.Phase.Should().Be(SagaPhase.Running.ToString());
        result.StartedAt.Should().Be(evt.Timestamp);
    }

    /// <summary>
    ///     Verifies that reducer throws when event is null.
    /// </summary>
    [Fact]
    public void ReduceWithNullEventThrowsArgumentNullException()
    {
        // Arrange
        TransferFundsSagaStatusProjection initial = new();

        // Act & Assert
        reducer.ShouldThrow<ArgumentNullException, SagaStartedEvent, TransferFundsSagaStatusProjection>(
            initial,
            null!,
            "eventData");
    }
}
