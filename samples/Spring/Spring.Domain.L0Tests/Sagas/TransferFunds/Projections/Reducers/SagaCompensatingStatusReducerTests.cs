using Mississippi.EventSourcing.Sagas.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions.Events;

using Spring.Domain.Sagas.TransferFunds.Projections;
using Spring.Domain.Sagas.TransferFunds.Projections.Reducers;


namespace Spring.Domain.L0Tests.Sagas.TransferFunds.Projections.Reducers;

/// <summary>
///     Tests for <see cref="SagaCompensatingStatusReducer" />.
/// </summary>
public sealed class SagaCompensatingStatusReducerTests
{
    private readonly SagaCompensatingStatusReducer reducer = new();

    /// <summary>
    ///     Verifies that applying SagaCompensating marks the saga as compensating.
    /// </summary>
    [Fact]
    public void ReduceWithSagaCompensatingMarksCompensating()
    {
        // Arrange
        TransferFundsSagaStatusProjection initial = new()
        {
            Phase = SagaPhase.Running.ToString(),
            CurrentStep = null,
        };
        SagaCompensatingEvent evt = new("CreditDestinationAccount", new DateTimeOffset(2024, 2, 1, 0, 5, 0, TimeSpan.Zero));

        // Act
        TransferFundsSagaStatusProjection result = reducer.Apply(initial, evt);

        // Assert
        result.Phase.Should().Be(SagaPhase.Compensating.ToString());
        result.CurrentStep.Should().BeNull();
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
        reducer.ShouldThrow<ArgumentNullException, SagaCompensatingEvent, TransferFundsSagaStatusProjection>(
            initial,
            null!,
            "eventData");
    }
}
