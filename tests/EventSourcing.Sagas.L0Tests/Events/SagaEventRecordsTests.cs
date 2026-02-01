using System;

using Mississippi.EventSourcing.Sagas.Abstractions.Events;


namespace Mississippi.EventSourcing.Sagas.L0Tests.Events;

/// <summary>
///     Tests for saga event records to ensure proper instantiation and property access.
/// </summary>
public sealed class SagaEventRecordsTests
{
    /// <summary>
    ///     SagaCompensatingEvent can be created with from step and timestamp.
    /// </summary>
    [Fact]
    public void SagaCompensatingEventShouldExposeAllProperties()
    {
        // Arrange
        string fromStep = "ProcessPayment";
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;

        // Act
        SagaCompensatingEvent evt = new(fromStep, timestamp);

        // Assert
        Assert.Equal(fromStep, evt.FromStep);
        Assert.Equal(timestamp, evt.Timestamp);
    }

    /// <summary>
    ///     SagaCompletedEvent can be created with timestamp.
    /// </summary>
    [Fact]
    public void SagaCompletedEventShouldExposeTimestamp()
    {
        // Arrange
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;

        // Act
        SagaCompletedEvent evt = new(timestamp);

        // Assert
        Assert.Equal(timestamp, evt.Timestamp);
    }

    /// <summary>
    ///     SagaCompletedEvent implements record equality.
    /// </summary>
    [Fact]
    public void SagaCompletedEventShouldImplementRecordEquality()
    {
        // Arrange
        DateTimeOffset ts = DateTimeOffset.UtcNow;
        SagaCompletedEvent evt1 = new(ts);
        SagaCompletedEvent evt2 = new(ts);

        // Assert
        Assert.Equal(evt1, evt2);
    }

    /// <summary>
    ///     SagaFailedEvent can be created with reason and timestamp.
    /// </summary>
    [Fact]
    public void SagaFailedEventShouldExposeAllProperties()
    {
        // Arrange
        string reason = "Payment declined";
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;

        // Act
        SagaFailedEvent evt = new(reason, timestamp);

        // Assert
        Assert.Equal(reason, evt.Reason);
        Assert.Equal(timestamp, evt.Timestamp);
    }

    /// <summary>
    ///     SagaStartedEvent can be created with all parameters.
    /// </summary>
    [Fact]
    public void SagaStartedEventShouldExposeAllProperties()
    {
        // Arrange
        string sagaId = Guid.NewGuid().ToString();
        string sagaType = "OrderSaga";
        string stepHash = "abc123";
        string correlationId = "corr-1";
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;

        // Act
        SagaStartedEvent evt = new(sagaId, sagaType, stepHash, correlationId, timestamp);

        // Assert
        Assert.Equal(sagaId, evt.SagaId);
        Assert.Equal(sagaType, evt.SagaType);
        Assert.Equal(stepHash, evt.StepHash);
        Assert.Equal(correlationId, evt.CorrelationId);
        Assert.Equal(timestamp, evt.Timestamp);
    }

    /// <summary>
    ///     SagaStartedEvent implements record equality.
    /// </summary>
    [Fact]
    public void SagaStartedEventShouldImplementRecordEquality()
    {
        // Arrange
        DateTimeOffset ts = DateTimeOffset.UtcNow;
        SagaStartedEvent evt1 = new("id1", "Type", "hash", "corr", ts);
        SagaStartedEvent evt2 = new("id1", "Type", "hash", "corr", ts);

        // Assert
        Assert.Equal(evt1, evt2);
    }

    /// <summary>
    ///     SagaStartedEvent supports null correlation ID.
    /// </summary>
    [Fact]
    public void SagaStartedEventShouldSupportNullCorrelationId()
    {
        // Arrange & Act
        SagaStartedEvent evt = new(Guid.NewGuid().ToString(), "Saga", "hash", null, DateTimeOffset.UtcNow);

        // Assert
        Assert.Null(evt.CorrelationId);
    }

    /// <summary>
    ///     SagaStepCompensatedEvent can be created with all parameters.
    /// </summary>
    [Fact]
    public void SagaStepCompensatedEventShouldExposeAllProperties()
    {
        // Arrange
        string stepName = "ProcessPayment";
        int stepOrder = 2;
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;

        // Act
        SagaStepCompensatedEvent evt = new(stepName, stepOrder, timestamp);

        // Assert
        Assert.Equal(stepName, evt.StepName);
        Assert.Equal(stepOrder, evt.StepOrder);
        Assert.Equal(timestamp, evt.Timestamp);
    }

    /// <summary>
    ///     SagaStepCompensationFailedEvent can be created with all parameters.
    /// </summary>
    [Fact]
    public void SagaStepCompensationFailedEventShouldExposeAllProperties()
    {
        // Arrange
        string stepName = "ProcessPayment";
        int stepOrder = 2;
        string errorCode = "REFUND_FAILED";
        string errorMessage = "Payment provider unavailable";
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;

        // Act
        SagaStepCompensationFailedEvent evt = new(stepName, stepOrder, errorCode, errorMessage, timestamp);

        // Assert
        Assert.Equal(stepName, evt.StepName);
        Assert.Equal(stepOrder, evt.StepOrder);
        Assert.Equal(errorCode, evt.ErrorCode);
        Assert.Equal(errorMessage, evt.ErrorMessage);
        Assert.Equal(timestamp, evt.Timestamp);
    }

    /// <summary>
    ///     SagaStepCompensationFailedEvent supports null error message.
    /// </summary>
    [Fact]
    public void SagaStepCompensationFailedEventShouldSupportNullErrorMessage()
    {
        // Arrange & Act
        SagaStepCompensationFailedEvent evt = new("Step", 1, "ERROR", null, DateTimeOffset.UtcNow);

        // Assert
        Assert.Null(evt.ErrorMessage);
    }

    /// <summary>
    ///     SagaStepCompletedEvent can be created with all parameters.
    /// </summary>
    [Fact]
    public void SagaStepCompletedEventShouldExposeAllProperties()
    {
        // Arrange
        string stepName = "ProcessPayment";
        int stepOrder = 2;
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;

        // Act
        SagaStepCompletedEvent evt = new(stepName, stepOrder, timestamp);

        // Assert
        Assert.Equal(stepName, evt.StepName);
        Assert.Equal(stepOrder, evt.StepOrder);
        Assert.Equal(timestamp, evt.Timestamp);
    }

    /// <summary>
    ///     SagaStepFailedEvent can be created with all parameters.
    /// </summary>
    [Fact]
    public void SagaStepFailedEventShouldExposeAllProperties()
    {
        // Arrange
        string stepName = "ProcessPayment";
        int stepOrder = 2;
        string errorCode = "PAYMENT_DECLINED";
        string errorMessage = "Insufficient funds";
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;

        // Act
        SagaStepFailedEvent evt = new(stepName, stepOrder, errorCode, errorMessage, timestamp);

        // Assert
        Assert.Equal(stepName, evt.StepName);
        Assert.Equal(stepOrder, evt.StepOrder);
        Assert.Equal(errorCode, evt.ErrorCode);
        Assert.Equal(errorMessage, evt.ErrorMessage);
        Assert.Equal(timestamp, evt.Timestamp);
    }

    /// <summary>
    ///     SagaStepFailedEvent supports null error message.
    /// </summary>
    [Fact]
    public void SagaStepFailedEventShouldSupportNullErrorMessage()
    {
        // Arrange & Act
        SagaStepFailedEvent evt = new("Step", 1, "ERROR", null, DateTimeOffset.UtcNow);

        // Assert
        Assert.Null(evt.ErrorMessage);
    }

    /// <summary>
    ///     SagaStepRetryEvent can be created with all parameters.
    /// </summary>
    [Fact]
    public void SagaStepRetryEventShouldExposeAllProperties()
    {
        // Arrange
        string stepName = "ProcessPayment";
        int stepOrder = 2;
        int attemptNumber = 3;
        int maxAttempts = 5;
        string previousErrorCode = "TIMEOUT";
        string previousErrorMessage = "Connection timed out";
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;

        // Act
        SagaStepRetryEvent evt = new(
            stepName,
            stepOrder,
            attemptNumber,
            maxAttempts,
            previousErrorCode,
            previousErrorMessage,
            timestamp);

        // Assert
        Assert.Equal(stepName, evt.StepName);
        Assert.Equal(stepOrder, evt.StepOrder);
        Assert.Equal(attemptNumber, evt.AttemptNumber);
        Assert.Equal(maxAttempts, evt.MaxAttempts);
        Assert.Equal(previousErrorCode, evt.PreviousErrorCode);
        Assert.Equal(previousErrorMessage, evt.PreviousErrorMessage);
        Assert.Equal(timestamp, evt.Timestamp);
    }

    /// <summary>
    ///     SagaStepRetryEvent supports null previous error message.
    /// </summary>
    [Fact]
    public void SagaStepRetryEventShouldSupportNullPreviousErrorMessage()
    {
        // Arrange & Act
        SagaStepRetryEvent evt = new("Step", 1, 2, 5, "ERROR", null, DateTimeOffset.UtcNow);

        // Assert
        Assert.Null(evt.PreviousErrorMessage);
    }

    /// <summary>
    ///     SagaStepStartedEvent can be created with all parameters.
    /// </summary>
    [Fact]
    public void SagaStepStartedEventShouldExposeAllProperties()
    {
        // Arrange
        string stepName = "ValidateOrder";
        int stepOrder = 1;
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;

        // Act
        SagaStepStartedEvent evt = new(stepName, stepOrder, timestamp);

        // Assert
        Assert.Equal(stepName, evt.StepName);
        Assert.Equal(stepOrder, evt.StepOrder);
        Assert.Equal(timestamp, evt.Timestamp);
    }
}