using System;

using Mississippi.EventSourcing.Sagas.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions.Events;
using Mississippi.EventSourcing.Sagas.L0Tests.Helpers;
using Mississippi.EventSourcing.Sagas.Reducers;


namespace Mississippi.EventSourcing.Sagas.L0Tests.Reducers;

/// <summary>
///     Unit tests for <see cref="SagaCompensatingReducer{TSaga}" />.
/// </summary>
public sealed class SagaCompensatingReducerTests
{
    /// <summary>
    ///     Verifies Reduce throws when event is null.
    /// </summary>
    [Fact]
    public void ReduceShouldThrowWhenEventIsNull()
    {
        // Arrange
        SagaCompensatingReducer<TestSagaState> sut = new();
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
        SagaCompensatingReducer<TestSagaState> sut = new();
        SagaCompensatingEvent eventData = new("Step1", DateTimeOffset.UtcNow);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => sut.Reduce(null!, eventData));
    }

    /// <summary>
    ///     Verifies Reduce transitions phase to Compensating.
    /// </summary>
    [Fact]
    public void ReduceShouldTransitionPhaseToCompensating()
    {
        // Arrange
        SagaCompensatingReducer<TestSagaState> sut = new();
        TestSagaState state = new()
        {
            Phase = SagaPhase.Running,
        };
        SagaCompensatingEvent eventData = new("ProcessPayment", DateTimeOffset.UtcNow);

        // Act
        TestSagaState result = sut.Reduce(state, eventData);

        // Assert
        Assert.Equal(SagaPhase.Compensating, result.Phase);
    }
}