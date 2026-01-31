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
    private static async Task ConsumeEventsAsync(
        SagaStepCompletedEffect<TestSagaState> effect,
        SagaStepCompletedEvent eventData,
        TestSagaState state,
        CancellationToken cancellationToken
    )
    {
        await foreach (object evt in effect.HandleAsync(eventData, state, "saga-brook", 1, cancellationToken))
        {
            // Consume events
            _ = evt;
        }
    }

    /// <summary>
    ///     HandleAsync applies post-step delay when DelayAfterStep attribute is present.
    /// </summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    [Fact]
    public async Task HandleAsyncShouldApplyDelayWhenAttributeIsPresent()
    {
        // Arrange
        const int delayMs = 5000;
        Mock<ISagaStepRegistry<TestSagaState>> registryMock = new();
        FakeTimeProvider timeProvider = new();
        Mock<ILogger<SagaStepCompletedEffect<TestSagaState>>> loggerMock = new();
        registryMock.Setup(r => r.Steps)
            .Returns(
            [
                new SagaStepInfo
                {
                    Name = "DelayedStep",
                    Order = 1,
                    StepType = typeof(DelayedTestStep),
                },
                new SagaStepInfo
                {
                    Name = "Step2",
                    Order = 2,
                    StepType = typeof(object),
                },
            ]);
        SagaStepCompletedEffect<TestSagaState> sut = new(registryMock.Object, timeProvider, loggerMock.Object);
        SagaStepCompletedEvent eventData = new("DelayedStep", 1, DateTimeOffset.UtcNow);
        TestSagaState state = new()
        {
            SagaId = Guid.NewGuid(),
        };

        // Act - Start the handle task
        Task handleTask = ConsumeEventsAsync(sut, eventData, state, CancellationToken.None);

        // Give the task time to start the delay
        await Task.Delay(50);

        // Assert the task is still running (delayed)
        Assert.False(handleTask.IsCompleted);

        // Advance time past the delay
        timeProvider.Advance(TimeSpan.FromMilliseconds(delayMs + 100));

        // Now the task should complete
        await handleTask;
        Assert.True(handleTask.IsCompleted);
    }

    /// <summary>
    ///     HandleAsync emits SagaStepStartedEvent when more steps remain.
    /// </summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
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
    /// <returns>A task that completes when the assertion finishes.</returns>
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
    /// <returns>A task that completes when the assertion finishes.</returns>
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

    /// <summary>
    ///     HandleAsync skips delay when DelayAfterStep attribute is absent.
    /// </summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    [Fact]
    public async Task HandleAsyncShouldNotDelayWhenAttributeIsAbsent()
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
                    Name = "NormalStep",
                    Order = 1,
                    StepType = typeof(SuccessfulTestStep), // No DelayAfterStep attribute
                },
                new SagaStepInfo
                {
                    Name = "Step2",
                    Order = 2,
                    StepType = typeof(object),
                },
            ]);
        SagaStepCompletedEffect<TestSagaState> sut = new(registryMock.Object, timeProvider, loggerMock.Object);
        SagaStepCompletedEvent eventData = new("NormalStep", 1, DateTimeOffset.UtcNow);
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

        // Assert - should complete immediately without advancing time
        Assert.Single(results);
        Assert.IsType<SagaStepStartedEvent>(results[0]);
    }

    /// <summary>
    ///     HandleAsync respects cancellation during delay.
    /// </summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    [Fact]
    public async Task HandleAsyncShouldRespectCancellationDuringDelay()
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
                    Name = "DelayedStep",
                    Order = 1,
                    StepType = typeof(DelayedTestStep),
                },
                new SagaStepInfo
                {
                    Name = "Step2",
                    Order = 2,
                    StepType = typeof(object),
                },
            ]);
        SagaStepCompletedEffect<TestSagaState> sut = new(registryMock.Object, timeProvider, loggerMock.Object);
        SagaStepCompletedEvent eventData = new("DelayedStep", 1, DateTimeOffset.UtcNow);
        TestSagaState state = new()
        {
            SagaId = Guid.NewGuid(),
        };
        using CancellationTokenSource cts = new();

        // Act & Assert - cancellation should throw
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            // Cancel after a short delay via timer
            _ = Task.Run(async () =>
            {
                await Task.Delay(50);
                await cts.CancelAsync();
            });
            await foreach (object evt in sut.HandleAsync(eventData, state, "saga-brook", 1, cts.Token))
            {
                _ = evt;
            }
        });
    }
}