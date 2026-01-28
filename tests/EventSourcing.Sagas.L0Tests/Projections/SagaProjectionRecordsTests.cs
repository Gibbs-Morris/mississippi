using System;
using System.Collections.Immutable;

using Mississippi.EventSourcing.Sagas.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions.Projections;


namespace Mississippi.EventSourcing.Sagas.L0Tests.Projections;

/// <summary>
///     Tests for saga projection records to ensure proper instantiation and property access.
/// </summary>
public sealed class SagaProjectionRecordsTests
{
    /// <summary>
    ///     SagaStatusProjection can be created with all properties.
    /// </summary>
    [Fact]
    public void SagaStatusProjectionShouldExposeAllProperties()
    {
        // Arrange
        string sagaId = Guid.NewGuid().ToString();
        string sagaType = "OrderSaga";
        SagaPhase phase = SagaPhase.Running;
        DateTimeOffset startedAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        SagaStepRecord currentStep = new("ProcessPayment", 2, DateTimeOffset.UtcNow, StepOutcome.Started);
        ImmutableList<SagaStepRecord> completedSteps =
            [new("ValidateOrder", 1, DateTimeOffset.UtcNow.AddMinutes(-3), StepOutcome.Succeeded)];

        // Act
        SagaStatusProjection projection = new()
        {
            SagaId = sagaId,
            SagaType = sagaType,
            Phase = phase,
            StartedAt = startedAt,
            CurrentStep = currentStep,
            CompletedSteps = completedSteps,
        };

        // Assert
        Assert.Equal(sagaId, projection.SagaId);
        Assert.Equal(sagaType, projection.SagaType);
        Assert.Equal(phase, projection.Phase);
        Assert.Equal(startedAt, projection.StartedAt);
        Assert.Equal(currentStep, projection.CurrentStep);
        Assert.Single(projection.CompletedSteps);
    }

    /// <summary>
    ///     SagaStatusProjection can be created with default values.
    /// </summary>
    [Fact]
    public void SagaStatusProjectionShouldHaveDefaults()
    {
        // Arrange & Act
        SagaStatusProjection projection = new();

        // Assert
        Assert.Equal(string.Empty, projection.SagaId);
        Assert.Equal(string.Empty, projection.SagaType);
        Assert.Equal(SagaPhase.NotStarted, projection.Phase);
        Assert.Null(projection.StartedAt);
        Assert.Null(projection.CompletedAt);
        Assert.Null(projection.FailureReason);
        Assert.Null(projection.CurrentStep);
        Assert.Empty(projection.CompletedSteps);
        Assert.Empty(projection.FailedSteps);
    }

    /// <summary>
    ///     SagaStatusProjection implements record equality.
    /// </summary>
    [Fact]
    public void SagaStatusProjectionShouldImplementRecordEquality()
    {
        // Arrange
        SagaStatusProjection projection1 = new()
        {
            SagaId = "test",
            SagaType = "OrderSaga",
        };
        SagaStatusProjection projection2 = new()
        {
            SagaId = "test",
            SagaType = "OrderSaga",
        };

        // Assert
        Assert.Equal(projection1, projection2);
    }

    /// <summary>
    ///     SagaStatusProjection can be created with completed state.
    /// </summary>
    [Fact]
    public void SagaStatusProjectionShouldSupportCompletedState()
    {
        // Arrange
        DateTimeOffset completedAt = DateTimeOffset.UtcNow;

        // Act
        SagaStatusProjection projection = new()
        {
            Phase = SagaPhase.Completed,
            CompletedAt = completedAt,
        };

        // Assert
        Assert.Equal(SagaPhase.Completed, projection.Phase);
        Assert.Equal(completedAt, projection.CompletedAt);
    }

    /// <summary>
    ///     SagaStatusProjection can be created with failed state.
    /// </summary>
    [Fact]
    public void SagaStatusProjectionShouldSupportFailedState()
    {
        // Arrange
        string failureReason = "Payment declined";
        SagaStepRecord failedStep = new(
            "ProcessPayment",
            2,
            DateTimeOffset.UtcNow,
            StepOutcome.Failed,
            "Insufficient funds");
        ImmutableList<SagaStepRecord> failedSteps = [failedStep];

        // Act
        SagaStatusProjection projection = new()
        {
            Phase = SagaPhase.Failed,
            FailureReason = failureReason,
            FailedSteps = failedSteps,
        };

        // Assert
        Assert.Equal(SagaPhase.Failed, projection.Phase);
        Assert.Equal(failureReason, projection.FailureReason);
        Assert.Single(projection.FailedSteps);
    }

    /// <summary>
    ///     SagaStepRecord can be created with all parameters.
    /// </summary>
    [Fact]
    public void SagaStepRecordShouldExposeAllProperties()
    {
        // Arrange
        string stepName = "ProcessPayment";
        int stepOrder = 2;
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;
        StepOutcome outcome = StepOutcome.Succeeded;
        string? errorMessage = null;

        // Act
        SagaStepRecord record = new(stepName, stepOrder, timestamp, outcome, errorMessage);

        // Assert
        Assert.Equal(stepName, record.StepName);
        Assert.Equal(stepOrder, record.StepOrder);
        Assert.Equal(timestamp, record.Timestamp);
        Assert.Equal(outcome, record.Outcome);
        Assert.Null(record.ErrorMessage);
    }

    /// <summary>
    ///     SagaStepRecord implements record equality.
    /// </summary>
    [Fact]
    public void SagaStepRecordShouldImplementRecordEquality()
    {
        // Arrange
        DateTimeOffset ts = DateTimeOffset.UtcNow;
        SagaStepRecord record1 = new("Step", 1, ts, StepOutcome.Succeeded);
        SagaStepRecord record2 = new("Step", 1, ts, StepOutcome.Succeeded);

        // Assert
        Assert.Equal(record1, record2);
    }

    /// <summary>
    ///     SagaStepRecord can be created with error message.
    /// </summary>
    [Fact]
    public void SagaStepRecordShouldSupportErrorMessage()
    {
        // Arrange
        string errorMessage = "Payment declined";

        // Act
        SagaStepRecord record = new("Step", 1, DateTimeOffset.UtcNow, StepOutcome.Failed, errorMessage);

        // Assert
        Assert.Equal(errorMessage, record.ErrorMessage);
        Assert.Equal(StepOutcome.Failed, record.Outcome);
    }
}