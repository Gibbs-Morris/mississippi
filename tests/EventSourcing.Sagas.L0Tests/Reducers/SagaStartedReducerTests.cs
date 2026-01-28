using System;

using Mississippi.EventSourcing.Sagas.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions.Events;
using Mississippi.EventSourcing.Sagas.L0Tests.Helpers;
using Mississippi.EventSourcing.Sagas.Reducers;


namespace Mississippi.EventSourcing.Sagas.L0Tests.Reducers;

/// <summary>
///     Unit tests for <see cref="SagaStartedReducer{TSaga}" />.
/// </summary>
public sealed class SagaStartedReducerTests
{
    /// <summary>
    ///     Verifies Reduce applies saga started fields to state.
    /// </summary>
    [Fact]
    public void ReduceShouldApplySagaStartedFields()
    {
        // Arrange
        SagaStartedReducer<TestSagaState> sut = new();
        TestSagaState state = new();
        Guid sagaId = Guid.NewGuid();
        SagaStartedEvent eventData = new(
            sagaId.ToString(),
            "TestSaga",
            "hash123",
            "test-correlation",
            DateTimeOffset.UtcNow);

        // Act
        TestSagaState result = sut.Reduce(state, eventData);

        // Assert
        Assert.Equal(sagaId, result.SagaId);
        Assert.Equal("test-correlation", result.CorrelationId);
        Assert.Equal("hash123", result.StepHash);
        Assert.Equal(SagaPhase.Running, result.Phase);
        Assert.Equal(-1, result.LastCompletedStepIndex);
        Assert.Equal(1, result.CurrentStepAttempt);
    }

    /// <summary>
    ///     Verifies Reduce uses empty GUID when saga ID is not a valid GUID.
    /// </summary>
    [Fact]
    public void ReduceShouldUseEmptyGuidForInvalidSagaId()
    {
        // Arrange
        SagaStartedReducer<TestSagaState> sut = new();
        TestSagaState state = new();
        SagaStartedEvent eventData = new(
            "not-a-guid",
            "TestSaga",
            "hash",
            null,
            DateTimeOffset.UtcNow);

        // Act
        TestSagaState result = sut.Reduce(state, eventData);

        // Assert
        Assert.Equal(Guid.Empty, result.SagaId);
    }

    /// <summary>
    ///     Verifies Reduce throws when event is null.
    /// </summary>
    [Fact]
    public void ReduceShouldThrowWhenEventIsNull()
    {
        // Arrange
        SagaStartedReducer<TestSagaState> sut = new();
        TestSagaState state = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => sut.Reduce(state, null!));
    }

    /// <summary>
    ///     Verifies Reduce throws when state is null.
    /// </summary>
    [Fact]
    public void ReduceShouldThrowWhenStateIsNull()
    {
        // Arrange
        SagaStartedReducer<TestSagaState> sut = new();
        SagaStartedEvent eventData = new(
            Guid.NewGuid().ToString(),
            "TestSaga",
            "hash",
            null,
            DateTimeOffset.UtcNow);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => sut.Reduce(null!, eventData));
    }
}
