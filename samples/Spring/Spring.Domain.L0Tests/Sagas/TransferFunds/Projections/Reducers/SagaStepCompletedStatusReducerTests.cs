using Mississippi.EventSourcing.Sagas.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions.Events;

using Spring.Domain.Sagas.TransferFunds.Projections;
using Spring.Domain.Sagas.TransferFunds.Projections.Reducers;


namespace Spring.Domain.L0Tests.Sagas.TransferFunds.Projections.Reducers;

/// <summary>
///     Tests for <see cref="SagaStepCompletedStatusReducer" />.
/// </summary>
public sealed class SagaStepCompletedStatusReducerTests
{
    private readonly SagaStepCompletedStatusReducer reducer = new();

    /// <summary>
    ///     Verifies that reducer throws when event is null.
    /// </summary>
    [Fact]
    public void ReduceWithNullEventThrowsArgumentNullException()
    {
        // Arrange
        TransferFundsSagaStatusProjection initial = new();

        // Act & Assert
        reducer.ShouldThrow<ArgumentNullException, SagaStepCompletedEvent, TransferFundsSagaStatusProjection>(
            initial,
            null!,
            "eventData");
    }

    /// <summary>
    ///     Verifies that applying SagaStepCompleted adds a completed step and clears current step.
    /// </summary>
    [Fact]
    public void ReduceWithSagaStepCompletedAddsCompletedStep()
    {
        // Arrange
        DateTimeOffset startedAt = new(2024, 2, 1, 0, 1, 0, TimeSpan.Zero);
        DateTimeOffset completedAt = new(2024, 2, 1, 0, 2, 0, TimeSpan.Zero);
        TransferFundsSagaStatusProjection initial = new()
        {
            CurrentStep = new()
            {
                StepName = "DebitSourceAccount",
                StepOrder = 1,
                Timestamp = startedAt,
                Outcome = StepOutcome.Started.ToString(),
            },
        };
        SagaStepCompletedEvent evt = new("DebitSourceAccount", 1, completedAt);

        // Act
        TransferFundsSagaStatusProjection result = reducer.Apply(initial, evt);

        // Assert
        result.CurrentStep.Should().BeNull();
        result.CompletedSteps.Should()
            .Contain(
                new TransferFundsSagaStepStatus
                {
                    StepName = "DebitSourceAccount",
                    StepOrder = 1,
                    Timestamp = completedAt,
                    Outcome = StepOutcome.Succeeded.ToString(),
                });
    }
}