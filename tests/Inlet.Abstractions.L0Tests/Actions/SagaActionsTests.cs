using System;
using System.Reflection;

using Mississippi.Inlet.Client.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Inlet.Abstractions.L0Tests.Actions;

/// <summary>
///     Tests for saga action interfaces.
/// </summary>
public sealed class SagaActionsTests
{
    /// <summary>
    ///     Test implementation of ISagaAction.
    /// </summary>
    private sealed record TestSagaAction(Guid SagaId, string? CorrelationId) : ISagaAction;

    /// <summary>
    ///     Test implementation of ISagaExecutingAction.
    /// </summary>
    private sealed record TestSagaExecutingAction(Guid SagaId, string SagaType, DateTimeOffset Timestamp)
        : ISagaExecutingAction<TestSagaExecutingAction>
    {
        public static TestSagaExecutingAction Create(
            Guid sagaId,
            string sagaType,
            DateTimeOffset timestamp
        ) =>
            new(sagaId, sagaType, timestamp);
    }

    /// <summary>
    ///     Test implementation of ISagaFailedAction.
    /// </summary>
    private sealed record TestSagaFailedAction(
        Guid SagaId,
        string? ErrorCode,
        string? ErrorMessage,
        DateTimeOffset Timestamp
    ) : ISagaFailedAction<TestSagaFailedAction>
    {
        public static TestSagaFailedAction Create(
            Guid sagaId,
            string? errorCode,
            string? errorMessage,
            DateTimeOffset timestamp
        ) =>
            new(sagaId, errorCode, errorMessage, timestamp);
    }

    /// <summary>
    ///     Test implementation of ISagaSucceededAction.
    /// </summary>
    private sealed record TestSagaSucceededAction(Guid SagaId, DateTimeOffset Timestamp)
        : ISagaSucceededAction<TestSagaSucceededAction>
    {
        public static TestSagaSucceededAction Create(
            Guid sagaId,
            DateTimeOffset timestamp
        ) =>
            new(sagaId, timestamp);
    }

    /// <summary>
    ///     Verifies that a concrete ISagaAction implementation works correctly.
    /// </summary>
    [Fact]
    public void ConcreteSagaActionImplementationWorks()
    {
        // Arrange
        Guid sagaId = Guid.NewGuid();
        string? correlationId = "test-correlation";

        // Act
        TestSagaAction sut = new(sagaId, correlationId);

        // Assert
        Assert.Equal(sagaId, sut.SagaId);
        Assert.Equal(correlationId, sut.CorrelationId);
    }

    /// <summary>
    ///     Verifies that a concrete ISagaExecutingAction implementation with factory works.
    /// </summary>
    [Fact]
    public void ConcreteSagaExecutingActionFactoryWorks()
    {
        // Arrange
        Guid sagaId = Guid.NewGuid();
        string sagaType = "TransferFunds";
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;

        // Act
        TestSagaExecutingAction sut = TestSagaExecutingAction.Create(sagaId, sagaType, timestamp);

        // Assert
        Assert.Equal(sagaId, sut.SagaId);
        Assert.Equal(sagaType, sut.SagaType);
        Assert.Equal(timestamp, sut.Timestamp);
    }

    /// <summary>
    ///     Verifies that a concrete ISagaFailedAction implementation with factory works.
    /// </summary>
    [Fact]
    public void ConcreteSagaFailedActionFactoryWorks()
    {
        // Arrange
        Guid sagaId = Guid.NewGuid();
        string? errorCode = "INSUFFICIENT_FUNDS";
        string? errorMessage = "Not enough balance";
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;

        // Act
        TestSagaFailedAction sut = TestSagaFailedAction.Create(sagaId, errorCode, errorMessage, timestamp);

        // Assert
        Assert.Equal(sagaId, sut.SagaId);
        Assert.Equal(errorCode, sut.ErrorCode);
        Assert.Equal(errorMessage, sut.ErrorMessage);
        Assert.Equal(timestamp, sut.Timestamp);
    }

    /// <summary>
    ///     Verifies that a concrete ISagaSucceededAction implementation with factory works.
    /// </summary>
    [Fact]
    public void ConcreteSagaSucceededActionFactoryWorks()
    {
        // Arrange
        Guid sagaId = Guid.NewGuid();
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;

        // Act
        TestSagaSucceededAction sut = TestSagaSucceededAction.Create(sagaId, timestamp);

        // Assert
        Assert.Equal(sagaId, sut.SagaId);
        Assert.Equal(timestamp, sut.Timestamp);
    }

    /// <summary>
    ///     Verifies that ISagaAction has CorrelationId property of type string?.
    /// </summary>
    [Fact]
    public void ISagaActionHasCorrelationIdProperty()
    {
        // Arrange
        PropertyInfo? property = typeof(ISagaAction).GetProperty(nameof(ISagaAction.CorrelationId));

        // Assert
        Assert.NotNull(property);
        Assert.Equal(typeof(string), property.PropertyType);
    }

    /// <summary>
    ///     Verifies that ISagaAction has SagaId property of type Guid.
    /// </summary>
    [Fact]
    public void ISagaActionHasSagaIdProperty()
    {
        // Arrange
        PropertyInfo? property = typeof(ISagaAction).GetProperty(nameof(ISagaAction.SagaId));

        // Assert
        Assert.NotNull(property);
        Assert.Equal(typeof(Guid), property.PropertyType);
    }

    /// <summary>
    ///     Verifies that ISagaAction inherits from IAction.
    /// </summary>
    [Fact]
    public void ISagaActionInheritsFromIAction()
    {
        // Assert
        Assert.True(typeof(ISagaAction).IsAssignableTo(typeof(IAction)));
    }

    /// <summary>
    ///     Verifies that ISagaExecutingAction has SagaId property of type Guid.
    /// </summary>
    [Fact]
    public void ISagaExecutingActionHasSagaIdProperty()
    {
        // Arrange
        PropertyInfo? property = typeof(ISagaExecutingAction).GetProperty(nameof(ISagaExecutingAction.SagaId));

        // Assert
        Assert.NotNull(property);
        Assert.Equal(typeof(Guid), property.PropertyType);
    }

    /// <summary>
    ///     Verifies that ISagaExecutingAction has SagaType property.
    /// </summary>
    [Fact]
    public void ISagaExecutingActionHasSagaTypeProperty()
    {
        // Arrange
        PropertyInfo? property = typeof(ISagaExecutingAction).GetProperty(nameof(ISagaExecutingAction.SagaType));

        // Assert
        Assert.NotNull(property);
        Assert.Equal(typeof(string), property.PropertyType);
    }

    /// <summary>
    ///     Verifies that ISagaExecutingAction has Timestamp property.
    /// </summary>
    [Fact]
    public void ISagaExecutingActionHasTimestampProperty()
    {
        // Arrange
        PropertyInfo? property = typeof(ISagaExecutingAction).GetProperty(nameof(ISagaExecutingAction.Timestamp));

        // Assert
        Assert.NotNull(property);
        Assert.Equal(typeof(DateTimeOffset), property.PropertyType);
    }

    /// <summary>
    ///     Verifies that ISagaFailedAction has ErrorCode property.
    /// </summary>
    [Fact]
    public void ISagaFailedActionHasErrorCodeProperty()
    {
        // Arrange
        PropertyInfo? property = typeof(ISagaFailedAction).GetProperty(nameof(ISagaFailedAction.ErrorCode));

        // Assert
        Assert.NotNull(property);
        Assert.Equal(typeof(string), property.PropertyType);
    }

    /// <summary>
    ///     Verifies that ISagaFailedAction has ErrorMessage property.
    /// </summary>
    [Fact]
    public void ISagaFailedActionHasErrorMessageProperty()
    {
        // Arrange
        PropertyInfo? property = typeof(ISagaFailedAction).GetProperty(nameof(ISagaFailedAction.ErrorMessage));

        // Assert
        Assert.NotNull(property);
        Assert.Equal(typeof(string), property.PropertyType);
    }

    /// <summary>
    ///     Verifies that ISagaFailedAction has SagaId property.
    /// </summary>
    [Fact]
    public void ISagaFailedActionHasSagaIdProperty()
    {
        // Arrange
        PropertyInfo? property = typeof(ISagaFailedAction).GetProperty(nameof(ISagaFailedAction.SagaId));

        // Assert
        Assert.NotNull(property);
        Assert.Equal(typeof(Guid), property.PropertyType);
    }

    /// <summary>
    ///     Verifies that ISagaFailedAction has Timestamp property.
    /// </summary>
    [Fact]
    public void ISagaFailedActionHasTimestampProperty()
    {
        // Arrange
        PropertyInfo? property = typeof(ISagaFailedAction).GetProperty(nameof(ISagaFailedAction.Timestamp));

        // Assert
        Assert.NotNull(property);
        Assert.Equal(typeof(DateTimeOffset), property.PropertyType);
    }

    /// <summary>
    ///     Verifies that ISagaSucceededAction has SagaId property.
    /// </summary>
    [Fact]
    public void ISagaSucceededActionHasSagaIdProperty()
    {
        // Arrange
        PropertyInfo? property = typeof(ISagaSucceededAction).GetProperty(nameof(ISagaSucceededAction.SagaId));

        // Assert
        Assert.NotNull(property);
        Assert.Equal(typeof(Guid), property.PropertyType);
    }

    /// <summary>
    ///     Verifies that ISagaSucceededAction has Timestamp property.
    /// </summary>
    [Fact]
    public void ISagaSucceededActionHasTimestampProperty()
    {
        // Arrange
        PropertyInfo? property = typeof(ISagaSucceededAction).GetProperty(nameof(ISagaSucceededAction.Timestamp));

        // Assert
        Assert.NotNull(property);
        Assert.Equal(typeof(DateTimeOffset), property.PropertyType);
    }
}