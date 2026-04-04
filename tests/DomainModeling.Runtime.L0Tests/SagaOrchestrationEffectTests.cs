using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using Mississippi.DomainModeling.Abstractions;


namespace Mississippi.DomainModeling.Runtime.L0Tests;

/// <summary>
///     Tests for <see cref="SagaOrchestrationEffect{TSaga}" />.
/// </summary>
public sealed class SagaOrchestrationEffectTests
{
    /// <summary>
    ///     Collects all events from an async stream.
    /// </summary>
    /// <param name="stream">The async stream.</param>
    /// <returns>The collected events.</returns>
    private static async Task<List<object>> CollectAsync(
        IAsyncEnumerable<object> stream
    )
    {
        List<object> results = [];
        await foreach (object item in stream)
        {
            results.Add(item);
        }

        return results;
    }

    /// <summary>
    ///     Creates a saga orchestration effect.
    /// </summary>
    /// <param name="steps">The configured saga steps.</param>
    /// <param name="provider">The service provider.</param>
    /// <param name="timeProvider">Optional time provider.</param>
    /// <returns>The effect instance.</returns>
    private static SagaOrchestrationEffect<TestSagaState> CreateEffect(
        IReadOnlyList<SagaStepInfo> steps,
        ServiceProvider provider,
        FakeTimeProvider? timeProvider = null
    )
    {
        SagaStepInfoProvider<TestSagaState> stepInfoProvider = new(steps);
        return new(
            stepInfoProvider,
            provider,
            timeProvider ?? new FakeTimeProvider(),
            NullLogger<SagaOrchestrationEffect<TestSagaState>>.Instance);
    }

    /// <summary>
    ///     Creates a service provider for saga orchestration tests.
    /// </summary>
    /// <param name="configureServices">Optional service configuration.</param>
    /// <returns>The configured service provider.</returns>
    private static ServiceProvider CreateProvider(
        Action<IServiceCollection>? configureServices = null
    )
    {
        ServiceCollection services = new();
        configureServices?.Invoke(services);
        return services.BuildServiceProvider();
    }

    /// <summary>
    ///     Asserts a saga step execution-started event has the expected metadata.
    /// </summary>
    /// <param name="item">The event to assert.</param>
    /// <param name="stepIndex">The expected step index.</param>
    /// <param name="stepName">The expected step name.</param>
    /// <param name="direction">The expected execution direction.</param>
    /// <returns>The asserted started event.</returns>
    private static SagaStepExecutionStarted AssertStartedEvent(
        object item,
        int stepIndex,
        string stepName,
        SagaExecutionDirection direction
    )
    {
        SagaStepExecutionStarted started = Assert.IsType<SagaStepExecutionStarted>(item);
        Assert.Equal(stepIndex, started.StepIndex);
        Assert.Equal(stepName, started.StepName);
        Assert.Equal(direction, started.Direction);
        Assert.Equal(SagaResumeSource.Initial, started.Source);
        return started;
    }

    /// <summary>
    ///     Verifies CanHandle accepts saga lifecycle events.
    /// </summary>
    [Fact]
    public void CanHandleRecognizesSagaEvents()
    {
        DateTimeOffset now = new(2025, 2, 12, 8, 0, 0, TimeSpan.Zero);
        using ServiceProvider provider = CreateProvider();
        SagaOrchestrationEffect<TestSagaState> effect = CreateEffect(Array.Empty<SagaStepInfo>(), provider);
        Assert.True(
            effect.CanHandle(
                new SagaStartedEvent
                {
                    SagaId = Guid.NewGuid(),
                    RecoveryMode = SagaRecoveryMode.Automatic,
                    StartedAt = now,
                    StepHash = "HASH",
                }));
        Assert.True(
            effect.CanHandle(
                new SagaStepCompleted
                {
                    AttemptId = Guid.NewGuid(),
                    StepIndex = 0,
                    StepName = "Step",
                    CompletedAt = now,
                    OperationKey = "operation-key",
                }));
        Assert.True(
            effect.CanHandle(
                new SagaStepFailed
                {
                    AttemptId = Guid.NewGuid(),
                    StepIndex = 0,
                    StepName = "Step",
                    ErrorCode = "ERR",
                    OperationKey = "operation-key",
                }));
        Assert.True(
            effect.CanHandle(
                new SagaCompensating
                {
                    FromStepIndex = 0,
                }));
        Assert.True(
            effect.CanHandle(
                new SagaStepCompensated
                {
                    AttemptId = Guid.NewGuid(),
                    OperationKey = "operation-key",
                    StepIndex = 0,
                    StepName = "Step",
                }));
        Assert.False(effect.CanHandle(new()));
    }

    /// <summary>
    ///     Verifies non-compensatable steps are treated as compensated.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task HandleAsyncCompensatesNonCompensatableStep()
    {
        SagaStepInfo[] steps =
        [
            new(0, "Debit", typeof(SagaNonCompensatableStep), false, SagaStepRecoveryPolicy.Automatic, null),
        ];
        using ServiceProvider provider = CreateProvider(services => services.AddTransient<SagaNonCompensatableStep>());
        SagaOrchestrationEffect<TestSagaState> effect = CreateEffect(steps, provider);
        List<object> events = await CollectAsync(
            effect.HandleAsync(
                new SagaCompensating
                {
                    FromStepIndex = 0,
                },
                new(),
                "saga",
                0,
                CancellationToken.None));
        Assert.Collection(
            events,
            item => AssertStartedEvent(item, 0, "Debit", SagaExecutionDirection.Compensation),
            item =>
            {
                SagaStepExecutionStarted started = Assert.IsType<SagaStepExecutionStarted>(events[0]);
                SagaStepCompensated compensated = Assert.IsType<SagaStepCompensated>(item);
                Assert.Equal(0, compensated.StepIndex);
                Assert.Equal("Debit", compensated.StepName);
                Assert.Equal(started.AttemptId, compensated.AttemptId);
                Assert.Equal(started.OperationKey, compensated.OperationKey);
            });
    }

    /// <summary>
    ///     Verifies compensated steps trigger previous compensation.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task HandleAsyncCompensatesPreviousStepWhenCompensated()
    {
        SagaStepInfo[] steps =
        [
            new(
                0,
                "Debit",
                typeof(SagaCompensationSuccessStep),
                true,
                SagaStepRecoveryPolicy.Automatic,
                SagaStepRecoveryPolicy.Automatic),
            new(
                1,
                "Credit",
                typeof(SagaNonCompensatableStep),
                false,
                SagaStepRecoveryPolicy.Automatic,
                null),
        ];
        using ServiceProvider provider = CreateProvider(services =>
        {
            services.AddTransient<SagaCompensationSuccessStep>();
            services.AddTransient<SagaNonCompensatableStep>();
        });
        SagaCompensationSuccessStep.Reset();
        SagaOrchestrationEffect<TestSagaState> effect = CreateEffect(steps, provider);
        List<object> events = await CollectAsync(
            effect.HandleAsync(
                new SagaStepCompensated
                {
                    AttemptId = Guid.NewGuid(),
                    OperationKey = "operation-key",
                    StepIndex = 1,
                    StepName = "Credit",
                },
                new(),
                "saga",
                0,
                CancellationToken.None));
        Assert.Collection(
            events,
            item => AssertStartedEvent(item, 0, "Debit", SagaExecutionDirection.Compensation),
            item =>
            {
                SagaStepExecutionStarted started = Assert.IsType<SagaStepExecutionStarted>(events[0]);
                SagaStepCompensated compensated = Assert.IsType<SagaStepCompensated>(item);
                Assert.Equal(0, compensated.StepIndex);
                Assert.Equal("Debit", compensated.StepName);
                Assert.Equal(started.AttemptId, compensated.AttemptId);
                Assert.Equal(started.OperationKey, compensated.OperationKey);
            });
        Assert.NotNull(SagaCompensationSuccessStep.LastCompensationContext);
        Assert.Equal(
            SagaExecutionDirection.Compensation,
            SagaCompensationSuccessStep.LastCompensationContext!.Direction);
        Assert.Equal(SagaResumeSource.Initial, SagaCompensationSuccessStep.LastCompensationContext.Source);
    }

    /// <summary>
    ///     Verifies compensation emits saga compensated for negative indices.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task HandleAsyncCompensatesWhenIndexNegative()
    {
        DateTimeOffset now = new(2025, 2, 12, 11, 0, 0, TimeSpan.Zero);
        FakeTimeProvider timeProvider = new(now);
        using ServiceProvider provider = CreateProvider();
        SagaOrchestrationEffect<TestSagaState> effect = CreateEffect(
            Array.Empty<SagaStepInfo>(),
            provider,
            timeProvider);
        List<object> events = await CollectAsync(
            effect.HandleAsync(
                new SagaCompensating
                {
                    FromStepIndex = -1,
                },
                new(),
                "saga",
                0,
                CancellationToken.None));
        SagaCompensated compensated = Assert.IsType<SagaCompensated>(Assert.Single(events));
        Assert.Equal(now, compensated.CompletedAt);
    }

    /// <summary>
    ///     Verifies a completed last step emits saga completed.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task HandleAsyncEmitsSagaCompletedWhenLastStepCompleted()
    {
        DateTimeOffset now = new(2025, 2, 12, 10, 0, 0, TimeSpan.Zero);
        FakeTimeProvider timeProvider = new(now);
        SagaStepInfo[] steps =
        [
            new(0, "Debit", typeof(SagaSuccessStep), false, SagaStepRecoveryPolicy.Automatic, null),
        ];
        using ServiceProvider provider = CreateProvider(services => services.AddTransient<SagaSuccessStep>());
        SagaOrchestrationEffect<TestSagaState> effect = CreateEffect(steps, provider, timeProvider);
        List<object> events = await CollectAsync(
            effect.HandleAsync(
                new SagaStepCompleted
                {
                    AttemptId = Guid.NewGuid(),
                    StepIndex = 0,
                    StepName = "Debit",
                    CompletedAt = now,
                    OperationKey = "operation-key",
                },
                new(),
                "saga",
                0,
                CancellationToken.None));
        SagaCompleted completed = Assert.IsType<SagaCompleted>(Assert.Single(events));
        Assert.Equal(now, completed.CompletedAt);
    }

    /// <summary>
    ///     Verifies step failures emit failure and compensating events.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task HandleAsyncEmitsStepFailedAndCompensatingWhenStepFails()
    {
        SagaStepInfo[] steps =
        [
            new(0, "Debit", typeof(SagaFailStep), false, SagaStepRecoveryPolicy.Automatic, null),
        ];
        using ServiceProvider provider = CreateProvider(services => services.AddTransient<SagaFailStep>());
        DateTimeOffset startedAt = new(2025, 2, 12, 12, 0, 0, TimeSpan.Zero);
        FakeTimeProvider timeProvider = new(startedAt);
        SagaOrchestrationEffect<TestSagaState> effect = CreateEffect(steps, provider, timeProvider);
        List<object> events = await CollectAsync(
            effect.HandleAsync(
                new SagaStartedEvent
                {
                    SagaId = Guid.NewGuid(),
                    RecoveryMode = SagaRecoveryMode.Automatic,
                    StartedAt = startedAt,
                    StepHash = "HASH",
                },
                new(),
                "saga",
                0,
                CancellationToken.None));
        Assert.Collection(
            events,
            item =>
            {
                SagaStepExecutionStarted started = AssertStartedEvent(item, 0, "Debit", SagaExecutionDirection.Forward);
                Assert.Equal(startedAt, started.StartedAt);
            },
            item =>
            {
                SagaStepExecutionStarted started = Assert.IsType<SagaStepExecutionStarted>(events[0]);
                SagaStepFailed failed = Assert.IsType<SagaStepFailed>(item);
                Assert.Equal("ERR", failed.ErrorCode);
                Assert.Equal("boom", failed.ErrorMessage);
                Assert.Equal(0, failed.StepIndex);
                Assert.Equal(started.AttemptId, failed.AttemptId);
                Assert.Equal(started.OperationKey, failed.OperationKey);
            },
            item =>
            {
                SagaCompensating compensating = Assert.IsType<SagaCompensating>(item);
                Assert.Equal(-1, compensating.FromStepIndex);
            });
    }

    /// <summary>
    ///     Verifies completed steps trigger the next step execution when available.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task HandleAsyncExecutesNextStepWhenStepCompleted()
    {
        DateTimeOffset now = new(2025, 2, 12, 10, 15, 0, TimeSpan.Zero);
        FakeTimeProvider timeProvider = new(now);
        SagaStepInfo[] steps =
        [
            new(0, "Debit", typeof(SagaSuccessStep), false, SagaStepRecoveryPolicy.Automatic, null),
            new(1, "Credit", typeof(SagaSuccessStep), false, SagaStepRecoveryPolicy.Automatic, null),
        ];
        using ServiceProvider provider = CreateProvider(services => services.AddTransient<SagaSuccessStep>());
        SagaOrchestrationEffect<TestSagaState> effect = CreateEffect(steps, provider, timeProvider);
        List<object> events = await CollectAsync(
            effect.HandleAsync(
                new SagaStepCompleted
                {
                    AttemptId = Guid.NewGuid(),
                    StepIndex = 0,
                    StepName = "Debit",
                    CompletedAt = now,
                    OperationKey = "operation-key",
                },
                new(),
                "saga",
                0,
                CancellationToken.None));
        Assert.Collection(
            events,
            item => AssertStartedEvent(item, 1, "Credit", SagaExecutionDirection.Forward),
            item => Assert.IsType<SagaMarkerEvent>(item),
            item =>
            {
                SagaStepExecutionStarted started = Assert.IsType<SagaStepExecutionStarted>(events[0]);
                SagaStepCompleted completed = Assert.IsType<SagaStepCompleted>(item);
                Assert.Equal(1, completed.StepIndex);
                Assert.Equal("Credit", completed.StepName);
                Assert.Equal(now, completed.CompletedAt);
                Assert.Equal(started.AttemptId, completed.AttemptId);
                Assert.Equal(started.OperationKey, completed.OperationKey);
            });
    }

    /// <summary>
    ///     Verifies saga started events execute the first step and emit completion.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task HandleAsyncExecutesStepAndEmitsCompletion()
    {
        DateTimeOffset now = new(2025, 2, 12, 9, 0, 0, TimeSpan.Zero);
        FakeTimeProvider timeProvider = new(now);
        SagaStepInfo[] steps =
        [
            new(0, "Debit", typeof(SagaSuccessStep), false, SagaStepRecoveryPolicy.Automatic, null),
        ];
        using ServiceProvider provider = CreateProvider(services => services.AddSingleton<SagaSuccessStep>());
        SagaSuccessStep step = provider.GetRequiredService<SagaSuccessStep>();
        SagaOrchestrationEffect<TestSagaState> effect = CreateEffect(steps, provider, timeProvider);
        List<object> events = await CollectAsync(
            effect.HandleAsync(
                new SagaStartedEvent
                {
                    SagaId = Guid.NewGuid(),
                    RecoveryMode = SagaRecoveryMode.Automatic,
                    StartedAt = now,
                    StepHash = "HASH",
                },
                new(),
                "saga",
                0,
                CancellationToken.None));
        Assert.Collection(
            events,
            item => AssertStartedEvent(item, 0, "Debit", SagaExecutionDirection.Forward),
            item => Assert.IsType<SagaMarkerEvent>(item),
            item =>
            {
                SagaStepExecutionStarted started = Assert.IsType<SagaStepExecutionStarted>(events[0]);
                SagaStepCompleted completed = Assert.IsType<SagaStepCompleted>(item);
                Assert.Equal(0, completed.StepIndex);
                Assert.Equal("Debit", completed.StepName);
                Assert.Equal(now, completed.CompletedAt);
                Assert.Equal(started.AttemptId, completed.AttemptId);
                Assert.Equal(started.OperationKey, completed.OperationKey);
            });
        Assert.NotNull(step.LastExecutionContext);
        Assert.Equal(SagaExecutionDirection.Forward, step.LastExecutionContext!.Direction);
        Assert.False(step.LastExecutionContext.IsReplay);
        Assert.Equal(SagaResumeSource.Initial, step.LastExecutionContext.Source);
        Assert.Equal(0, step.LastExecutionContext.StepIndex);
        Assert.Equal("Debit", step.LastExecutionContext.StepName);
    }

    /// <summary>
    ///     Verifies compensation fails when step metadata is missing.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task HandleAsyncFailsWhenCompensatingStepMissing()
    {
        FakeTimeProvider timeProvider = new();
        using ServiceProvider provider = CreateProvider();
        SagaOrchestrationEffect<TestSagaState> effect = CreateEffect(
            Array.Empty<SagaStepInfo>(),
            provider,
            timeProvider);
        List<object> events = await CollectAsync(
            effect.HandleAsync(
                new SagaCompensating
                {
                    FromStepIndex = 0,
                },
                new(),
                "saga",
                0,
                CancellationToken.None));
        SagaFailed failed = Assert.IsType<SagaFailed>(Assert.Single(events));
        Assert.Equal("COMPENSATION_FAILED", failed.ErrorCode);
        Assert.NotEqual(default, failed.FailedAt);
    }

    /// <summary>
    ///     Verifies compensation failure emits saga failed.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task HandleAsyncFailsWhenCompensationFails()
    {
        SagaStepInfo[] steps =
        [
            new(
                0,
                "Debit",
                typeof(SagaCompensationFailStep),
                true,
                SagaStepRecoveryPolicy.Automatic,
                SagaStepRecoveryPolicy.Automatic),
        ];
        using ServiceProvider provider = CreateProvider(services => services.AddTransient<SagaCompensationFailStep>());
        SagaOrchestrationEffect<TestSagaState> effect = CreateEffect(steps, provider);
        List<object> events = await CollectAsync(
            effect.HandleAsync(
                new SagaCompensating
                {
                    FromStepIndex = 0,
                },
                new(),
                "saga",
                0,
                CancellationToken.None));
        Assert.Collection(
            events,
            item => AssertStartedEvent(item, 0, "Debit", SagaExecutionDirection.Compensation),
            item =>
            {
                SagaFailed failed = Assert.IsType<SagaFailed>(item);
                Assert.Equal("FAIL", failed.ErrorCode);
                Assert.Equal("nope", failed.ErrorMessage);
            });
    }

    /// <summary>
    ///     Verifies saga step failed events yield no output.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task HandleAsyncReturnsEmptyForStepFailedEvent()
    {
        using ServiceProvider provider = CreateProvider();
        SagaOrchestrationEffect<TestSagaState> effect = CreateEffect(Array.Empty<SagaStepInfo>(), provider);
        List<object> events = await CollectAsync(
            effect.HandleAsync(
                new SagaStepFailed
                {
                    AttemptId = Guid.NewGuid(),
                    StepIndex = 0,
                    StepName = "Step",
                    ErrorCode = "ERR",
                    OperationKey = "operation-key",
                },
                new(),
                "saga",
                0,
                CancellationToken.None));
        Assert.Empty(events);
    }

    /// <summary>
    ///     Verifies unsupported events yield no output.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task HandleAsyncReturnsEmptyForUnsupportedEvent()
    {
        using ServiceProvider provider = CreateProvider();
        SagaOrchestrationEffect<TestSagaState> effect = CreateEffect(Array.Empty<SagaStepInfo>(), provider);
        List<object> events = await CollectAsync(effect.HandleAsync(new(), new(), "saga", 0, CancellationToken.None));
        Assert.Empty(events);
    }

    /// <summary>
    ///     Verifies missing steps yield no events.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task HandleAsyncSkipsWhenNoStepsConfigured()
    {
        using ServiceProvider provider = CreateProvider();
        SagaOrchestrationEffect<TestSagaState> effect = CreateEffect(Array.Empty<SagaStepInfo>(), provider);
        List<object> events = await CollectAsync(
            effect.HandleAsync(
                new SagaStartedEvent
                {
                    SagaId = Guid.NewGuid(),
                    RecoveryMode = SagaRecoveryMode.Automatic,
                    StartedAt = new(2025, 2, 12, 14, 0, 0, TimeSpan.Zero),
                    StepHash = "HASH",
                },
                new(),
                "saga",
                0,
                CancellationToken.None));
        SagaFailed failure = Assert.IsType<SagaFailed>(Assert.Single(events));
        Assert.Equal("STEP_METADATA_MISSING", failure.ErrorCode);
    }

    /// <summary>
    ///     Verifies invalid step types throw during execution.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task HandleAsyncThrowsWhenStepTypeInvalid()
    {
        SagaStepInfo[] steps =
        [
            new(0, "Invalid", typeof(SagaInvalidStep), false, SagaStepRecoveryPolicy.Automatic, null),
        ];
        using ServiceProvider provider = CreateProvider(services => services.AddTransient<SagaInvalidStep>());
        SagaOrchestrationEffect<TestSagaState> effect = CreateEffect(steps, provider);
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (object item in effect.HandleAsync(
                               new SagaStartedEvent
                               {
                                   SagaId = Guid.NewGuid(),
                                   RecoveryMode = SagaRecoveryMode.Automatic,
                                   StartedAt = new(2025, 2, 12, 13, 0, 0, TimeSpan.Zero),
                                   StepHash = "HASH",
                               },
                               new(),
                               "saga",
                               0,
                               CancellationToken.None))
            {
                Assert.NotNull(item);
            }
        });
    }

    /// <summary>
    ///     Verifies skipped compensation emits saga step compensated.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task HandleAsyncTreatsSkippedCompensationAsCompensated()
    {
        SagaStepInfo[] steps =
        [
            new(
                0,
                "Debit",
                typeof(SagaCompensationSkippedStep),
                true,
                SagaStepRecoveryPolicy.Automatic,
                SagaStepRecoveryPolicy.Automatic),
        ];
        using ServiceProvider provider =
            CreateProvider(services => services.AddTransient<SagaCompensationSkippedStep>());
        SagaOrchestrationEffect<TestSagaState> effect = CreateEffect(steps, provider);
        List<object> events = await CollectAsync(
            effect.HandleAsync(
                new SagaCompensating
                {
                    FromStepIndex = 0,
                },
                new(),
                "saga",
                0,
                CancellationToken.None));
        Assert.Collection(
            events,
            item => AssertStartedEvent(item, 0, "Debit", SagaExecutionDirection.Compensation),
            item =>
            {
                SagaStepExecutionStarted started = Assert.IsType<SagaStepExecutionStarted>(events[0]);
                SagaStepCompensated compensated = Assert.IsType<SagaStepCompensated>(item);
                Assert.Equal(0, compensated.StepIndex);
                Assert.Equal("Debit", compensated.StepName);
                Assert.Equal(started.AttemptId, compensated.AttemptId);
                Assert.Equal(started.OperationKey, compensated.OperationKey);
            });
    }

    /// <summary>
    ///     Verifies missing compensation error codes fall back to defaults.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task HandleAsyncUsesDefaultErrorCodeWhenCompensationMissingCode()
    {
        SagaStepInfo[] steps =
        [
            new(
                0,
                "Debit",
                typeof(SagaCompensationNoCodeStep),
                true,
                SagaStepRecoveryPolicy.Automatic,
                SagaStepRecoveryPolicy.Automatic),
        ];
        using ServiceProvider provider = CreateProvider(services =>
            services.AddTransient<SagaCompensationNoCodeStep>());
        SagaOrchestrationEffect<TestSagaState> effect = CreateEffect(steps, provider);
        List<object> events = await CollectAsync(
            effect.HandleAsync(
                new SagaCompensating
                {
                    FromStepIndex = 0,
                },
                new(),
                "saga",
                0,
                CancellationToken.None));
        Assert.Collection(
            events,
            item => AssertStartedEvent(item, 0, "Debit", SagaExecutionDirection.Compensation),
            item =>
            {
                SagaFailed failed = Assert.IsType<SagaFailed>(item);
                Assert.Equal("COMPENSATION_FAILED", failed.ErrorCode);
                Assert.Equal("nope", failed.ErrorMessage);
            });
    }
}