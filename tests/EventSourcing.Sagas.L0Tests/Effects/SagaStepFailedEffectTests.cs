using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

using Mississippi.EventSourcing.Sagas.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions.Events;
using Mississippi.EventSourcing.Sagas.Effects;
using Mississippi.EventSourcing.Sagas.L0Tests.Helpers;

using Moq;


namespace Mississippi.EventSourcing.Sagas.L0Tests.Effects;

/// <summary>
///     Tests for <see cref="SagaStepFailedEffect{TSaga}" />.
/// </summary>
public sealed class SagaStepFailedEffectTests
{
    private static async Task<List<object>> CollectEventsAsync<TSaga>(
        SagaStepFailedEffect<TSaga> sut,
        SagaStepFailedEvent eventData,
        TSaga state
    )
        where TSaga : class
    {
        List<object> results = [];
        await foreach (object evt in sut.HandleAsync(eventData, state, "saga-brook", 1, CancellationToken.None))
        {
            results.Add(evt);
        }

        return results;
    }

    private static (SagaStepFailedEffect<TestSagaState> Effect, ServiceProvider Provider) CreateEffect(
        List<SagaStepInfo> steps
    )
    {
        ServiceCollection services = new();
        foreach (SagaStepInfo step in steps)
        {
            if (step.CompensationType is not null)
            {
                services.AddTransient(step.CompensationType);
            }
        }

        ServiceProvider provider = services.BuildServiceProvider();
        Mock<ISagaStepRegistry<TestSagaState>> registryMock = new();
        registryMock.Setup(r => r.Steps).Returns(steps);
        FakeTimeProvider timeProvider = new();
        Mock<ILogger<SagaStepFailedEffect<TestSagaState>>> loggerMock = new();
        SagaStepFailedEffect<TestSagaState> effect = new(
            registryMock.Object,
            provider,
            timeProvider,
            loggerMock.Object);
        return (effect, provider);
    }

    private static (SagaStepFailedEffect<TSaga> Effect, ServiceProvider Provider) CreateEffectForSaga<TSaga>(
        List<SagaStepInfo> steps
    )
        where TSaga : class
    {
        ServiceCollection services = new();
        foreach (SagaStepInfo step in steps)
        {
            if (step.CompensationType is not null)
            {
                services.AddTransient(step.CompensationType);
            }
        }

        ServiceProvider provider = services.BuildServiceProvider();
        Mock<ISagaStepRegistry<TSaga>> registryMock = new();
        registryMock.Setup(r => r.Steps).Returns(steps);
        FakeTimeProvider timeProvider = new();
        Mock<ILogger<SagaStepFailedEffect<TSaga>>> loggerMock = new();
        SagaStepFailedEffect<TSaga> effect = new(registryMock.Object, provider, timeProvider, loggerMock.Object);
        return (effect, provider);
    }

    private static SagaStepInfo CreateStepInfo(
        string name,
        int order,
        Type compensationType
    ) =>
        new()
        {
            Name = name,
            Order = order,
            StepType = typeof(NoOpStep),
            CompensationType = compensationType,
        };

    /// <summary>
    ///     HandleAsync continues compensating other steps even when one fails.
    /// </summary>
    [Fact]
    public async Task HandleAsyncShouldContinueCompensatingAfterFailure()
    {
        // Arrange
        SagaStepInfo step1 = CreateStepInfo("Step1", 1, typeof(SuccessfulCompensation));
        SagaStepInfo step2 = CreateStepInfo("Step2", 2, typeof(FailingCompensation));
        SagaStepInfo step3 = CreateStepInfo("Step3", 3, typeof(SuccessfulCompensation));
        (SagaStepFailedEffect<TestSagaState> sut, ServiceProvider _) = CreateEffect([step1, step2, step3]);
        SagaStepFailedEvent eventData = new("Step4", 4, "FAILED", "Error", DateTimeOffset.UtcNow);
        TestSagaState state = new()
        {
            SagaId = Guid.NewGuid(),
            LastCompletedStepIndex = 2, // All three steps completed
        };

        // Act
        List<object> results = await CollectEventsAsync(sut, eventData, state);

        // Assert - should have compensation events for all three steps
        // (Step3: compensated, Step2: failed, Step1: compensated)
        bool hasStep1Compensated = false;
        bool hasStep2Failed = false;
        bool hasStep3Compensated = false;
        foreach (object evt in results)
        {
            if (evt is SagaStepCompensatedEvent compensated)
            {
                if (compensated.StepName == "Step1")
                {
                    hasStep1Compensated = true;
                }

                if (compensated.StepName == "Step3")
                {
                    hasStep3Compensated = true;
                }
            }

            if (evt is SagaStepCompensationFailedEvent failed && (failed.StepName == "Step2"))
            {
                hasStep2Failed = true;
            }
        }

        Assert.True(hasStep1Compensated, "Step1 should have been compensated");
        Assert.True(hasStep2Failed, "Step2 compensation should have failed");
        Assert.True(hasStep3Compensated, "Step3 should have been compensated");
    }

    /// <summary>
    ///     HandleAsync emits SagaStepCompensationFailedEvent when compensation fails.
    /// </summary>
    [Fact]
    public async Task HandleAsyncShouldEmitCompensationFailedEventOnError()
    {
        // Arrange
        SagaStepInfo step1 = CreateStepInfo("Step1", 1, typeof(FailingCompensation));
        (SagaStepFailedEffect<TestSagaState> sut, ServiceProvider _) = CreateEffect([step1]);
        SagaStepFailedEvent eventData = new("Step2", 2, "FAILED", "Error", DateTimeOffset.UtcNow);
        TestSagaState state = new()
        {
            SagaId = Guid.NewGuid(),
            LastCompletedStepIndex = 0,
        };

        // Act
        List<object> results = await CollectEventsAsync(sut, eventData, state);

        // Assert
        bool hasCompensationFailed = false;
        foreach (object evt in results)
        {
            if (evt is SagaStepCompensationFailedEvent)
            {
                hasCompensationFailed = true;
                break;
            }
        }

        Assert.True(hasCompensationFailed);
    }

    /// <summary>
    ///     HandleAsync extracts correct saga identity from ISagaState.
    /// </summary>
    [Fact]
    public async Task HandleAsyncShouldExtractSagaIdentityFromState()
    {
        // Arrange
        Guid expectedSagaId = Guid.NewGuid();
        SagaStepInfo step1 = CreateStepInfo("Step1", 1, typeof(SuccessfulCompensation));
        (SagaStepFailedEffect<TestSagaState> sut, ServiceProvider _) = CreateEffect([step1]);
        SagaStepFailedEvent eventData = new("Step2", 2, "FAILED", "Error", DateTimeOffset.UtcNow);
        TestSagaState state = new()
        {
            SagaId = expectedSagaId,
            CorrelationId = "test-correlation",
            StartedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            LastCompletedStepIndex = 0,
        };

        // Act
        List<object> results = await CollectEventsAsync(sut, eventData, state);

        // Assert - compensation should succeed (using saga context with identity)
        bool hasCompensated = false;
        foreach (object evt in results)
        {
            if (evt is SagaStepCompensatedEvent)
            {
                hasCompensated = true;
                break;
            }
        }

        Assert.True(hasCompensated);
    }

    /// <summary>
    ///     HandleAsync handles exception in compensation gracefully.
    /// </summary>
    [Fact]
    public async Task HandleAsyncShouldHandleCompensationException()
    {
        // Arrange
        SagaStepInfo step1 = CreateStepInfo("Step1", 1, typeof(ExceptionThrowingCompensation));
        (SagaStepFailedEffect<TestSagaState> sut, ServiceProvider _) = CreateEffect([step1]);
        SagaStepFailedEvent eventData = new("Step2", 2, "FAILED", "Error", DateTimeOffset.UtcNow);
        TestSagaState state = new()
        {
            SagaId = Guid.NewGuid(),
            LastCompletedStepIndex = 0,
        };

        // Act
        List<object> results = await CollectEventsAsync(sut, eventData, state);

        // Assert - should emit compensation failed event
        bool hasCompensationFailed = false;
        foreach (object evt in results)
        {
            if (evt is SagaStepCompensationFailedEvent failed)
            {
                hasCompensationFailed = true;
                Assert.Equal("COMPENSATION_EXCEPTION", failed.ErrorCode);
            }
        }

        Assert.True(hasCompensationFailed);
    }

    /// <summary>
    ///     HandleAsync includes error details in SagaFailedEvent message.
    /// </summary>
    [Fact]
    public async Task HandleAsyncShouldIncludeErrorDetailsInFailedEvent()
    {
        // Arrange
        (SagaStepFailedEffect<TestSagaState> sut, ServiceProvider _) = CreateEffect([]);
        SagaStepFailedEvent eventData = new(
            "ProcessPayment",
            3,
            "CARD_DECLINED",
            "Insufficient funds on card ending 4242",
            DateTimeOffset.UtcNow);
        TestSagaState state = new()
        {
            SagaId = Guid.NewGuid(),
        };

        // Act
        List<object> results = await CollectEventsAsync(sut, eventData, state);

        // Assert
        SagaFailedEvent failedEvent = Assert.IsType<SagaFailedEvent>(results[^1]);
        Assert.Contains("ProcessPayment", failedEvent.Reason);
        Assert.Contains("CARD_DECLINED", failedEvent.Reason);
        Assert.Contains("Insufficient funds", failedEvent.Reason);
    }

    /// <summary>
    ///     HandleAsync runs compensations in reverse order.
    /// </summary>
    [Fact]
    public async Task HandleAsyncShouldRunCompensationsInReverseOrder()
    {
        // Arrange
        SagaStepInfo step1 = CreateStepInfo("Step1", 1, typeof(SuccessfulCompensation));
        SagaStepInfo step2 = CreateStepInfo("Step2", 2, typeof(SuccessfulCompensation));
        SagaStepInfo step3 = CreateStepInfo("Step3", 3, typeof(SuccessfulCompensation));
        (SagaStepFailedEffect<TestSagaState> sut, ServiceProvider _) = CreateEffect([step1, step2, step3]);
        SagaStepFailedEvent eventData = new("Step3", 3, "FAILED", "Error", DateTimeOffset.UtcNow);
        TestSagaState state = new()
        {
            SagaId = Guid.NewGuid(),
            LastCompletedStepIndex = 1, // Step1 and Step2 completed
        };

        // Act
        List<object> results = await CollectEventsAsync(sut, eventData, state);

        // Assert - should compensate Step2 then Step1 (reverse order)
        List<SagaStepCompensatedEvent> compensatedEvents = [];
        foreach (object evt in results)
        {
            if (evt is SagaStepCompensatedEvent compensated)
            {
                compensatedEvents.Add(compensated);
            }
        }

        Assert.Equal(2, compensatedEvents.Count);
        Assert.Equal("Step2", compensatedEvents[0].StepName);
        Assert.Equal("Step1", compensatedEvents[1].StepName);
    }

    /// <summary>
    ///     HandleAsync skips steps without compensation types.
    /// </summary>
    [Fact]
    public async Task HandleAsyncShouldSkipStepsWithoutCompensation()
    {
        // Arrange
        SagaStepInfo step1 = new()
        {
            Name = "Step1",
            Order = 1,
            StepType = typeof(NoOpStep),
            CompensationType = null,
        };
        SagaStepInfo step2 = CreateStepInfo("Step2", 2, typeof(SuccessfulCompensation));
        (SagaStepFailedEffect<TestSagaState> sut, ServiceProvider _) = CreateEffect([step1, step2]);
        SagaStepFailedEvent eventData = new("Step2", 2, "FAILED", "Error", DateTimeOffset.UtcNow);
        TestSagaState state = new()
        {
            SagaId = Guid.NewGuid(),
            LastCompletedStepIndex = 0,
        };

        // Act
        List<object> results = await CollectEventsAsync(sut, eventData, state);

        // Assert - no compensation for Step1 (no CompensationType)
        List<SagaStepCompensatedEvent> compensatedEvents = [];
        foreach (object evt in results)
        {
            if (evt is SagaStepCompensatedEvent compensated)
            {
                compensatedEvents.Add(compensated);
            }
        }

        Assert.Empty(compensatedEvents);
    }

    /// <summary>
    ///     HandleAsync with Immediate strategy emits SagaCompensatingEvent, compensations, and SagaFailedEvent.
    /// </summary>
    [Fact]
    public async Task HandleAsyncWithImmediateStrategyShouldEmitCompensatingAndFailedEvents()
    {
        // Arrange - default TestSagaState has no SagaOptionsAttribute so uses Immediate
        SagaStepInfo step1 = CreateStepInfo("Step1", 1, typeof(SuccessfulCompensation));
        SagaStepInfo step2 = CreateStepInfo("Step2", 2, typeof(SuccessfulCompensation));
        (SagaStepFailedEffect<TestSagaState> sut, ServiceProvider _) = CreateEffect([step1, step2]);
        SagaStepFailedEvent eventData = new("Step2", 2, "ORDER_CANCELLED", "User cancelled", DateTimeOffset.UtcNow);
        TestSagaState state = new()
        {
            SagaId = Guid.NewGuid(),
            LastCompletedStepIndex = 0, // Step1 completed
        };

        // Act
        List<object> results = await CollectEventsAsync(sut, eventData, state);

        // Assert
        Assert.True(results.Count >= 2); // At least SagaCompensatingEvent and SagaFailedEvent
        Assert.IsType<SagaCompensatingEvent>(results[0]);
        Assert.IsType<SagaFailedEvent>(results[^1]);
    }

    /// <summary>
    ///     HandleAsync with Manual strategy emits only SagaFailedEvent without compensation.
    /// </summary>
    [Fact]
    public async Task HandleAsyncWithManualStrategyShouldOnlyEmitSagaFailedEvent()
    {
        // Arrange
        SagaStepInfo step1 = CreateStepInfo("Step1", 1, typeof(SuccessfulCompensation));
        (SagaStepFailedEffect<ManualStrategySaga> sut, ServiceProvider _) =
            CreateEffectForSaga<ManualStrategySaga>([step1]);
        SagaStepFailedEvent eventData = new("Step1", 1, "PAYMENT_FAILED", "Insufficient funds", DateTimeOffset.UtcNow);
        ManualStrategySaga state = new()
        {
            SagaId = Guid.NewGuid(),
        };

        // Act
        List<object> results = await CollectEventsAsync(sut, eventData, state);

        // Assert
        Assert.Single(results);
        SagaFailedEvent failedEvent = Assert.IsType<SagaFailedEvent>(results[0]);
        Assert.Contains("PAYMENT_FAILED", failedEvent.Reason);
    }

    /// <summary>
    ///     HandleAsync with RetryThenCompensate strategy emits compensation when max retries exceeded.
    /// </summary>
    [Fact]
    public async Task HandleAsyncWithRetryStrategyShouldCompensateWhenMaxRetriesExceeded()
    {
        // Arrange
        SagaStepInfo step1 = CreateStepInfo("Step1", 1, typeof(SuccessfulCompensation));
        (SagaStepFailedEffect<RetryStrategySaga> sut, ServiceProvider _) =
            CreateEffectForSaga<RetryStrategySaga>([step1]);
        SagaStepFailedEvent eventData = new("Step1", 1, "TIMEOUT", "Service unavailable", DateTimeOffset.UtcNow);
        RetryStrategySaga state = new()
        {
            SagaId = Guid.NewGuid(),
            CurrentStepAttempt = 3, // Already at max retries
        };

        // Act
        List<object> results = await CollectEventsAsync(sut, eventData, state);

        // Assert
        Assert.True(results.Count >= 2);
        Assert.IsType<SagaCompensatingEvent>(results[0]);
        Assert.IsType<SagaFailedEvent>(results[^1]);
    }

    /// <summary>
    ///     HandleAsync with RetryThenCompensate strategy emits retry event when under max retries.
    /// </summary>
    [Fact]
    public async Task HandleAsyncWithRetryStrategyShouldEmitRetryEventWhenUnderMaxRetries()
    {
        // Arrange
        SagaStepInfo step1 = CreateStepInfo("Step1", 1, typeof(SuccessfulCompensation));
        (SagaStepFailedEffect<RetryStrategySaga> sut, ServiceProvider _) =
            CreateEffectForSaga<RetryStrategySaga>([step1]);
        SagaStepFailedEvent eventData = new("Step1", 1, "TIMEOUT", "Service unavailable", DateTimeOffset.UtcNow);
        RetryStrategySaga state = new()
        {
            SagaId = Guid.NewGuid(),
            CurrentStepAttempt = 1, // First attempt
        };

        // Act
        List<object> results = await CollectEventsAsync(sut, eventData, state);

        // Assert
        Assert.Equal(2, results.Count);
        SagaStepRetryEvent retryEvent = Assert.IsType<SagaStepRetryEvent>(results[0]);
        Assert.Equal(2, retryEvent.AttemptNumber); // Next attempt is 2
        Assert.Equal(3, retryEvent.MaxAttempts);
        Assert.IsType<SagaStepStartedEvent>(results[1]); // Re-triggers the step
    }
}