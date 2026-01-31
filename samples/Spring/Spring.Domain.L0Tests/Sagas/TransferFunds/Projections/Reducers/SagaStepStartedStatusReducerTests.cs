using Mississippi.EventSourcing.Sagas.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions.Events;
using Spring.Domain.Sagas.TransferFunds.Projections;
using Spring.Domain.Sagas.TransferFunds.Projections.Reducers;


namespace Spring.Domain.L0Tests.Sagas.TransferFunds.Projections.Reducers;

/// <summary>
///     Tests for <see cref="SagaStepStartedStatusReducer" />.
/// </summary>
public sealed class SagaStepStartedStatusReducerTests
{
    private readonly SagaStepStartedStatusReducer reducer = new();

    /// <summary>
    ///     Verifies that applying SagaStepStarted sets the current step.
    /// </summary>
    [Fact]
    public void ReduceWithSagaStepStartedSetsCurrentStep()
    {
        // Arrange
        TransferFundsSagaStatusProjection initial = new();
        DateTimeOffset timestamp = new(2024, 2, 1, 0, 1, 0, TimeSpan.Zero);
        SagaStepStartedEvent evt = new("DebitSourceAccount", 1, timestamp);

        // Act
        TransferFundsSagaStatusProjection result = reducer.Apply(initial, evt);

        // Assert
        result.CurrentStep.Should().Be(new TransferFundsSagaStepStatus
        {
            StepName = "DebitSourceAccount",
            StepOrder = 1,
            Timestamp = timestamp,
            Outcome = StepOutcome.Started.ToString(),
        });
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
        reducer.ShouldThrow<ArgumentNullException, SagaStepStartedEvent, TransferFundsSagaStatusProjection>(
            initial,
            null!,
            "eventData");
    }
}
