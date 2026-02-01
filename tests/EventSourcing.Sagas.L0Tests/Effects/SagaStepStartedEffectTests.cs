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
///     Tests for <see cref="SagaStepStartedEffect{TSaga}" />.
/// </summary>
public sealed class SagaStepStartedEffectTests
{
    private static ServiceProvider CreateEffectWithStep(
        SagaStepInfo stepInfo,
        out SagaStepStartedEffect<TestSagaState> effect
    )
    {
        ServiceCollection services = new();
        services.AddTransient(stepInfo.StepType);
        ServiceProvider provider = services.BuildServiceProvider();
        Mock<ISagaStepRegistry<TestSagaState>> registryMock = new();
        registryMock.Setup(r => r.Steps).Returns([stepInfo]);
        registryMock.Setup(r => r.StepHash).Returns("test-hash");
        FakeTimeProvider timeProvider = new();
        Mock<ILogger<SagaStepStartedEffect<TestSagaState>>> loggerMock = new();
        effect = new(registryMock.Object, provider, timeProvider, loggerMock.Object);
        return provider;
    }

    /// <summary>
    ///     HandleAsync emits SagaStepCompletedEvent on successful step execution.
    /// </summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    [Fact]
    public async Task HandleAsyncShouldEmitStepCompletedOnSuccess()
    {
        // Arrange
        SagaStepInfo stepInfo = new()
        {
            Name = "TestStep",
            Order = 1,
            StepType = typeof(SuccessfulTestStep),
        };
        using ServiceProvider provider = CreateEffectWithStep(stepInfo, out SagaStepStartedEffect<TestSagaState> sut);
        SagaStepStartedEvent eventData = new("TestStep", 1, DateTimeOffset.UtcNow);
        TestSagaState state = new()
        {
            SagaId = Guid.NewGuid(),
            StepHash = "test-hash",
            LastCompletedStepIndex = -1,
        };

        // Act
        List<object> results = [];
        await foreach (object evt in sut.HandleAsync(eventData, state, "saga-brook", 1, CancellationToken.None))
        {
            results.Add(evt);
        }

        // Assert
        Assert.Single(results);
        Assert.IsType<SagaStepCompletedEvent>(results[0]);
    }

    /// <summary>
    ///     HandleAsync emits SagaStepFailedEvent on exception.
    /// </summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    [Fact]
    public async Task HandleAsyncShouldEmitStepFailedOnException()
    {
        // Arrange
        SagaStepInfo stepInfo = new()
        {
            Name = "TestStep",
            Order = 1,
            StepType = typeof(ExceptionThrowingTestStep),
        };
        using ServiceProvider provider = CreateEffectWithStep(stepInfo, out SagaStepStartedEffect<TestSagaState> sut);
        SagaStepStartedEvent eventData = new("TestStep", 1, DateTimeOffset.UtcNow);
        TestSagaState state = new()
        {
            SagaId = Guid.NewGuid(),
            StepHash = "test-hash",
            LastCompletedStepIndex = -1,
        };

        // Act
        List<object> results = [];
        await foreach (object evt in sut.HandleAsync(eventData, state, "saga-brook", 1, CancellationToken.None))
        {
            results.Add(evt);
        }

        // Assert
        Assert.Single(results);
        SagaStepFailedEvent failedEvent = Assert.IsType<SagaStepFailedEvent>(results[0]);
        Assert.Equal("STEP_EXCEPTION", failedEvent.ErrorCode);
    }

    /// <summary>
    ///     HandleAsync emits SagaStepFailedEvent on step failure.
    /// </summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    [Fact]
    public async Task HandleAsyncShouldEmitStepFailedOnFailure()
    {
        // Arrange
        SagaStepInfo stepInfo = new()
        {
            Name = "TestStep",
            Order = 1,
            StepType = typeof(FailingTestStep),
        };
        using ServiceProvider provider = CreateEffectWithStep(stepInfo, out SagaStepStartedEffect<TestSagaState> sut);
        SagaStepStartedEvent eventData = new("TestStep", 1, DateTimeOffset.UtcNow);
        TestSagaState state = new()
        {
            SagaId = Guid.NewGuid(),
            StepHash = "test-hash",
            LastCompletedStepIndex = -1,
        };

        // Act
        List<object> results = [];
        await foreach (object evt in sut.HandleAsync(eventData, state, "saga-brook", 1, CancellationToken.None))
        {
            results.Add(evt);
        }

        // Assert
        Assert.Single(results);
        SagaStepFailedEvent failedEvent = Assert.IsType<SagaStepFailedEvent>(results[0]);
        Assert.Equal("PAYMENT_DECLINED", failedEvent.ErrorCode);
    }

    /// <summary>
    ///     HandleAsync emits SagaStepFailedEvent on step hash mismatch.
    /// </summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    [Fact]
    public async Task HandleAsyncShouldEmitStepFailedOnHashMismatch()
    {
        // Arrange
        SagaStepInfo stepInfo = new()
        {
            Name = "TestStep",
            Order = 1,
            StepType = typeof(SuccessfulTestStep),
        };
        using ServiceProvider provider = CreateEffectWithStep(stepInfo, out SagaStepStartedEffect<TestSagaState> sut);
        SagaStepStartedEvent eventData = new("TestStep", 1, DateTimeOffset.UtcNow);
        TestSagaState state = new()
        {
            SagaId = Guid.NewGuid(),
            StepHash = "different-hash", // Mismatched hash
            LastCompletedStepIndex = -1,
        };

        // Act
        List<object> results = [];
        await foreach (object evt in sut.HandleAsync(eventData, state, "saga-brook", 1, CancellationToken.None))
        {
            results.Add(evt);
        }

        // Assert
        Assert.Single(results);
        SagaStepFailedEvent failedEvent = Assert.IsType<SagaStepFailedEvent>(results[0]);
        Assert.Equal("STEP_HASH_MISMATCH", failedEvent.ErrorCode);
    }

    /// <summary>
    ///     HandleAsync emits SagaStepFailedEvent when step not found.
    /// </summary>
    /// <remarks>
    ///     Note: When a step is not found, GetStepIndex returns -1. If LastCompletedStepIndex is also -1,
    ///     the idempotency guard (-1 >= -1) triggers early exit. To test the STEP_NOT_FOUND code path,
    ///     we need LastCompletedStepIndex to be at a valid index (0+) so the guard doesn't trigger.
    /// </remarks>
    /// <returns>A task that completes when the assertion finishes.</returns>
    [Fact]
    public async Task HandleAsyncShouldEmitStepFailedWhenStepNotFound()
    {
        // Arrange - use NonSagaState to bypass the ISagaState idempotency guard
        // The step-not-found path is only reachable when state doesn't implement ISagaState
        SagaStepInfo step1 = new()
        {
            Name = "ExistingStep",
            Order = 1,
            StepType = typeof(SuccessfulTestStep),
        };
        SagaStepInfo step2 = new()
        {
            Name = "AnotherStep",
            Order = 2,
            StepType = typeof(SuccessfulTestStep),
        };
        ServiceCollection services = new();
        services.AddTransient<SuccessfulTestStep>();
        using ServiceProvider provider = services.BuildServiceProvider();
        Mock<ISagaStepRegistry<NonSagaState>> registryMock = new();
        registryMock.Setup(r => r.Steps).Returns([step1, step2]);
        registryMock.Setup(r => r.StepHash).Returns(string.Empty);
        FakeTimeProvider timeProvider = new();
        Mock<ILogger<SagaStepStartedEffect<NonSagaState>>> loggerMock = new();
        SagaStepStartedEffect<NonSagaState> sut = new(registryMock.Object, provider, timeProvider, loggerMock.Object);

        // Request step order 99 which doesn't exist in the registry
        SagaStepStartedEvent eventData = new("NonExistentStep", 99, DateTimeOffset.UtcNow);
        NonSagaState state = new();

        // Act
        List<object> results = [];
        await foreach (object evt in sut.HandleAsync(eventData, state, "saga-brook", 1, CancellationToken.None))
        {
            results.Add(evt);
        }

        // Assert
        Assert.Single(results);
        SagaStepFailedEvent failedEvent = Assert.IsType<SagaStepFailedEvent>(results[0]);
        Assert.Equal("STEP_NOT_FOUND", failedEvent.ErrorCode);
    }

    /// <summary>
    ///     HandleAsync includes business events from step result.
    /// </summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    [Fact]
    public async Task HandleAsyncShouldIncludeBusinessEventsFromStepResult()
    {
        // Arrange
        SagaStepInfo stepInfo = new()
        {
            Name = "TestStep",
            Order = 1,
            StepType = typeof(EventEmittingTestStep),
        };
        using ServiceProvider provider = CreateEffectWithStep(stepInfo, out SagaStepStartedEffect<TestSagaState> sut);
        SagaStepStartedEvent eventData = new("TestStep", 1, DateTimeOffset.UtcNow);
        TestSagaState state = new()
        {
            SagaId = Guid.NewGuid(),
            StepHash = "test-hash",
            LastCompletedStepIndex = -1,
        };

        // Act
        List<object> results = [];
        await foreach (object evt in sut.HandleAsync(eventData, state, "saga-brook", 1, CancellationToken.None))
        {
            results.Add(evt);
        }

        // Assert
        Assert.Equal(2, results.Count);
        Assert.IsType<TestBusinessEvent>(results[0]);
        Assert.IsType<SagaStepCompletedEvent>(results[1]);
    }

    /// <summary>
    ///     HandleAsync reports correct step name and order in completion event.
    /// </summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    [Fact]
    public async Task HandleAsyncShouldIncludeStepDetailsInCompletionEvent()
    {
        // Arrange
        SagaStepInfo stepInfo = new()
        {
            Name = "ProcessPayment",
            Order = 3,
            StepType = typeof(SuccessfulTestStep),
        };
        using ServiceProvider provider = CreateEffectWithStep(stepInfo, out SagaStepStartedEffect<TestSagaState> sut);
        SagaStepStartedEvent eventData = new("ProcessPayment", 3, DateTimeOffset.UtcNow);
        TestSagaState state = new()
        {
            SagaId = Guid.NewGuid(),
            StepHash = "test-hash",
            LastCompletedStepIndex = -1,
        };

        // Act
        List<object> results = [];
        await foreach (object evt in sut.HandleAsync(eventData, state, "saga-brook", 1, CancellationToken.None))
        {
            results.Add(evt);
        }

        // Assert
        SagaStepCompletedEvent completedEvent = Assert.IsType<SagaStepCompletedEvent>(results[0]);
        Assert.Equal("ProcessPayment", completedEvent.StepName);
        Assert.Equal(3, completedEvent.StepOrder);
    }

    /// <summary>
    ///     HandleAsync skips execution for already completed step (idempotency).
    /// </summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    [Fact]
    public async Task HandleAsyncShouldSkipAlreadyCompletedStep()
    {
        // Arrange
        SagaStepInfo stepInfo = new()
        {
            Name = "TestStep",
            Order = 1,
            StepType = typeof(SuccessfulTestStep),
        };
        using ServiceProvider provider = CreateEffectWithStep(stepInfo, out SagaStepStartedEffect<TestSagaState> sut);
        SagaStepStartedEvent eventData = new("TestStep", 1, DateTimeOffset.UtcNow);
        TestSagaState state = new()
        {
            SagaId = Guid.NewGuid(),
            StepHash = "test-hash",
            LastCompletedStepIndex = 0, // Step 1 (index 0) already completed
        };

        // Act
        List<object> results = [];
        await foreach (object evt in sut.HandleAsync(eventData, state, "saga-brook", 1, CancellationToken.None))
        {
            results.Add(evt);
        }

        // Assert
        Assert.Empty(results);
    }

    /// <summary>
    ///     HandleAsync uses empty step hash when state has empty step hash.
    /// </summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    [Fact]
    public async Task HandleAsyncShouldSucceedWithEmptyStepHash()
    {
        // Arrange
        SagaStepInfo stepInfo = new()
        {
            Name = "TestStep",
            Order = 1,
            StepType = typeof(SuccessfulTestStep),
        };
        using ServiceProvider provider = CreateEffectWithStep(stepInfo, out SagaStepStartedEffect<TestSagaState> sut);
        SagaStepStartedEvent eventData = new("TestStep", 1, DateTimeOffset.UtcNow);
        TestSagaState state = new()
        {
            SagaId = Guid.NewGuid(),
            StepHash = string.Empty, // Empty step hash - should skip validation
            LastCompletedStepIndex = -1,
        };

        // Act
        List<object> results = [];
        await foreach (object evt in sut.HandleAsync(eventData, state, "saga-brook", 1, CancellationToken.None))
        {
            results.Add(evt);
        }

        // Assert
        Assert.Single(results);
        Assert.IsType<SagaStepCompletedEvent>(results[0]);
    }

    /// <summary>
    ///     HandleAsync uses null step hash when state has no step hash.
    /// </summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    [Fact]
    public async Task HandleAsyncShouldSucceedWithNullStepHash()
    {
        // Arrange
        SagaStepInfo stepInfo = new()
        {
            Name = "TestStep",
            Order = 1,
            StepType = typeof(SuccessfulTestStep),
        };
        using ServiceProvider provider = CreateEffectWithStep(stepInfo, out SagaStepStartedEffect<TestSagaState> sut);
        SagaStepStartedEvent eventData = new("TestStep", 1, DateTimeOffset.UtcNow);
        TestSagaState state = new()
        {
            SagaId = Guid.NewGuid(),
            StepHash = null, // No step hash - should skip validation
            LastCompletedStepIndex = -1,
        };

        // Act
        List<object> results = [];
        await foreach (object evt in sut.HandleAsync(eventData, state, "saga-brook", 1, CancellationToken.None))
        {
            results.Add(evt);
        }

        // Assert
        Assert.Single(results);
        Assert.IsType<SagaStepCompletedEvent>(results[0]);
    }

    /// <summary>
    ///     HandleAsync uses fallback identity when state does not implement ISagaState.
    /// </summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    [Fact]
    public async Task HandleAsyncShouldUseFallbackForNonSagaState()
    {
        // Arrange
        SagaStepInfo stepInfo = new()
        {
            Name = "TestStep",
            Order = 1,
            StepType = typeof(NonSagaStateSuccessfulStep),
        };
        ServiceCollection services = new();
        services.AddTransient<NonSagaStateSuccessfulStep>();
        using ServiceProvider provider = services.BuildServiceProvider();
        Mock<ISagaStepRegistry<NonSagaState>> registryMock = new();
        registryMock.Setup(r => r.Steps).Returns([stepInfo]);
        registryMock.Setup(r => r.StepHash).Returns("test-hash");
        FakeTimeProvider timeProvider = new();
        Mock<ILogger<SagaStepStartedEffect<NonSagaState>>> loggerMock = new();
        SagaStepStartedEffect<NonSagaState> sut = new(registryMock.Object, provider, timeProvider, loggerMock.Object);
        SagaStepStartedEvent eventData = new("TestStep", 1, DateTimeOffset.UtcNow);
        NonSagaState state = new();

        // Act
        List<object> results = [];
        await foreach (object evt in sut.HandleAsync(eventData, state, "saga-brook", 1, CancellationToken.None))
        {
            results.Add(evt);
        }

        // Assert - should still succeed using fallback identity
        Assert.Single(results);
        Assert.IsType<SagaStepCompletedEvent>(results[0]);
    }
}