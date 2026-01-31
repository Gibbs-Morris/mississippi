using Mississippi.EventSourcing.Sagas.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions.Events;

using Spring.Domain.Sagas.TransferFunds.Projections;
using Spring.Domain.Sagas.TransferFunds.Projections.Reducers;


namespace Spring.Domain.L0Tests.Sagas.TransferFunds.Projections.Reducers;

/// <summary>
///     Tests for <see cref="SagaFailedStatusReducer" />.
/// </summary>
public sealed class SagaFailedStatusReducerTests
{
    private readonly SagaFailedStatusReducer reducer = new();

    /// <summary>
    ///     Verifies that reducer throws when event is null.
    /// </summary>
    [Fact]
    public void ReduceWithNullEventThrowsArgumentNullException()
    {
        // Arrange
        TransferFundsSagaStatusProjection initial = new();

        // Act & Assert
        reducer.ShouldThrow<ArgumentNullException, SagaFailedEvent, TransferFundsSagaStatusProjection>(
            initial,
            null!,
            "eventData");
    }

    /// <summary>
    ///     Verifies that applying SagaFailed marks the saga as failed with reason.
    /// </summary>
    [Fact]
    public void ReduceWithSagaFailedMarksFailed()
    {
        // Arrange
        TransferFundsSagaStatusProjection initial = new()
        {
            Phase = SagaPhase.Running.ToString(),
            CurrentStep = null,
        };
        DateTimeOffset failedAt = new(2024, 2, 1, 0, 4, 0, TimeSpan.Zero);
        SagaFailedEvent evt = new("Insufficient funds", failedAt);

        // Act
        TransferFundsSagaStatusProjection result = reducer.Apply(initial, evt);

        // Assert
        result.Phase.Should().Be(SagaPhase.Failed.ToString());
        result.FailureReason.Should().Be("Insufficient funds");
        result.CompletedAt.Should().Be(failedAt);
        result.CurrentStep.Should().BeNull();
    }
}