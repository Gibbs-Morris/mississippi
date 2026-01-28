using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

using Mississippi.EventSourcing.Sagas.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions.Events;
using Mississippi.EventSourcing.Sagas.Effects;
using Mississippi.EventSourcing.Sagas.L0Tests.Helpers;

using Moq;


namespace Mississippi.EventSourcing.Sagas.L0Tests.Effects;

/// <summary>
///     Tests for <see cref="SagaStepCompletedEffect{TSaga}" />.
/// </summary>
public sealed class SagaStepCompletedEffectTests
{
    /// <summary>
    ///     HandleAsync emits SagaStepStartedEvent when more steps remain.
    /// </summary>
    [Fact]
    public async Task HandleAsyncShouldEmitNextStepStartedEventWhenMoreSteps()
    {
        // Arrange
        Mock<ISagaStepRegistry<TestSagaState>> registryMock = new();
        FakeTimeProvider timeProvider = new();
        Mock<ILogger<SagaStepCompletedEffect<TestSagaState>>> loggerMock = new();
        registryMock.Setup(r => r.Steps)
            .Returns(
            [
                new SagaStepInfo
                {
                    Name = "Step1",
                    Order = 1,
                    StepType = typeof(object),
                },
                new SagaStepInfo
                {
                    Name = "Step2",
                    Order = 2,
                    StepType = typeof(object),
                },
            ]);
        SagaStepCompletedEffect<TestSagaState> sut = new(registryMock.Object, timeProvider, loggerMock.Object);
        SagaStepCompletedEvent eventData = new("Step1", 1, DateTimeOffset.UtcNow);
        TestSagaState state = new()
        {
            SagaId = Guid.NewGuid(),
        };

        // Act
        List<object> results = [];
        await foreach (object evt in sut.HandleAsync(eventData, state, "saga-brook", 1, CancellationToken.None))
        {
            results.Add(evt);
        }

        // Assert
        Assert.Single(results);
        SagaStepStartedEvent nextStepEvent = Assert.IsType<SagaStepStartedEvent>(results[0]);
        Assert.Equal("Step2", nextStepEvent.StepName);
        Assert.Equal(2, nextStepEvent.StepOrder);
    }

    /// <summary>
    ///     HandleAsync emits SagaCompletedEvent when no more steps.
    /// </summary>
    [Fact]
    public async Task HandleAsyncShouldEmitSagaCompletedEventWhenNoMoreSteps()
    {
        // Arrange
        Mock<ISagaStepRegistry<TestSagaState>> registryMock = new();
        FakeTimeProvider timeProvider = new();
        Mock<ILogger<SagaStepCompletedEffect<TestSagaState>>> loggerMock = new();
        registryMock.Setup(r => r.Steps)
            .Returns(
            [
                new SagaStepInfo
                {
                    Name = "Step1",
                    Order = 1,
                    StepType = typeof(object),
                },
            ]);
        SagaStepCompletedEffect<TestSagaState> sut = new(registryMock.Object, timeProvider, loggerMock.Object);
        SagaStepCompletedEvent eventData = new("Step1", 1, DateTimeOffset.UtcNow);
        TestSagaState state = new()
        {
            SagaId = Guid.NewGuid(),
        };

        // Act
        List<object> results = [];
        await foreach (object evt in sut.HandleAsync(eventData, state, "saga-brook", 1, CancellationToken.None))
        {
            results.Add(evt);
        }

        // Assert
        Assert.Single(results);
        Assert.IsType<SagaCompletedEvent>(results[0]);
    }

    /// <summary>
    ///     HandleAsync finds next step correctly with non-sequential ordering.
    /// </summary>
    [Fact]
    public async Task HandleAsyncShouldFindNextStepWithNonSequentialOrdering()
    {
        // Arrange
        Mock<ISagaStepRegistry<TestSagaState>> registryMock = new();
        FakeTimeProvider timeProvider = new();
        Mock<ILogger<SagaStepCompletedEffect<TestSagaState>>> loggerMock = new();
        registryMock.Setup(r => r.Steps)
            .Returns(
            [
                new SagaStepInfo
                {
                    Name = "Step1",
                    Order = 10,
                    StepType = typeof(object),
                },
                new SagaStepInfo
                {
                    Name = "Step2",
                    Order = 20,
                    StepType = typeof(object),
                },
                new SagaStepInfo
                {
                    Name = "Step3",
                    Order = 30,
                    StepType = typeof(object),
                },
            ]);
        SagaStepCompletedEffect<TestSagaState> sut = new(registryMock.Object, timeProvider, loggerMock.Object);
        SagaStepCompletedEvent eventData = new("Step1", 10, DateTimeOffset.UtcNow);
        TestSagaState state = new()
        {
            SagaId = Guid.NewGuid(),
        };

        // Act
        List<object> results = [];
        await foreach (object evt in sut.HandleAsync(eventData, state, "saga-brook", 1, CancellationToken.None))
        {
            results.Add(evt);
        }

        // Assert
        Assert.Single(results);
        SagaStepStartedEvent nextStepEvent = Assert.IsType<SagaStepStartedEvent>(results[0]);
        Assert.Equal("Step2", nextStepEvent.StepName);
        Assert.Equal(20, nextStepEvent.StepOrder);
    }
}