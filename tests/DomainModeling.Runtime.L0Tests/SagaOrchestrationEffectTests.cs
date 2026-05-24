using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using Mississippi.DomainModeling.Abstractions;

using Moq;


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
    /// <param name="logger">Optional logger.</param>
    /// <returns>The effect instance.</returns>
    private static SagaOrchestrationEffect<TestSagaState> CreateEffect(
        IReadOnlyList<SagaStepInfo> steps,
        ServiceProvider provider,
        FakeTimeProvider? timeProvider = null,
        ILogger<SagaOrchestrationEffect<TestSagaState>>? logger = null
    )
    {
        SagaStepInfoProvider<TestSagaState> stepInfoProvider = new(steps);
        return new(
            stepInfoProvider,
            provider,
            timeProvider ?? new FakeTimeProvider(),
            logger ?? NullLogger<SagaOrchestrationEffect<TestSagaState>>.Instance);
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
    ///     Verifies an exception log entry was written with the expected exception and rendered fragments.
    /// </summary>
    /// <param name="loggerMock">The logger mock.</param>
    /// <param name="logLevel">The expected log level.</param>
    /// <param name="eventId">The expected event id.</param>
    /// <param name="expectedExceptionMessage">The expected exception message.</param>
    /// <param name="expectedMessageFragments">The rendered message fragments that must be present.</param>
    private static void VerifyExceptionLog(
        Mock<ILogger<SagaOrchestrationEffect<TestSagaState>>> loggerMock,
        LogLevel logLevel,
        int eventId,
        string expectedExceptionMessage,
        params string[] expectedMessageFragments
    )
    {
        loggerMock.Verify(
            l => l.Log(
                logLevel,
                It.Is<EventId>(id => id.Id == eventId),
                It.Is<It.IsAnyType>((
                    state,
                    _
                ) => expectedMessageFragments.All(fragment =>
                    state.ToString()!.Contains(fragment, StringComparison.Ordinal))),
                It.Is<Exception>(ex => ex.Message == expectedExceptionMessage),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    ///     Verifies a log entry was written with the expected level, event id, and rendered fragments.
    /// </summary>
    /// <param name="loggerMock">The logger mock.</param>
    /// <param name="logLevel">The expected log level.</param>
    /// <param name="eventId">The expected event id.</param>
    /// <param name="expectedMessageFragments">The rendered message fragments that must be present.</param>
    private static void VerifyLog(
        Mock<ILogger<SagaOrchestrationEffect<TestSagaState>>> loggerMock,
        LogLevel logLevel,
        int eventId,
        params string[] expectedMessageFragments
    )
    {
        loggerMock.Verify(
            l => l.Log(
                logLevel,
                It.Is<EventId>(id => id.Id == eventId),
                It.Is<It.IsAnyType>((
                    state,
                    _
                ) => expectedMessageFragments.All(fragment =>
                    state.ToString()!.Contains(fragment, StringComparison.Ordinal))),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    ///     Verifies a log entry was not written for an event id.
    /// </summary>
    /// <param name="loggerMock">The logger mock.</param>
    /// <param name="logLevel">The log level.</param>
    /// <param name="eventId">The event id.</param>
    private static void VerifyNoLog(
        Mock<ILogger<SagaOrchestrationEffect<TestSagaState>>> loggerMock,
        LogLevel logLevel,
        int eventId
    )
    {
        loggerMock.Verify(
            l => l.Log(
                logLevel,
                It.Is<EventId>(id => id.Id == eventId),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
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
                    StartedAt = now,
                    StepHash = "HASH",
                }));
        Assert.True(
            effect.CanHandle(
                new SagaStepCompleted
                {
                    StepIndex = 0,
                    StepName = "Step",
                    CompletedAt = now,
                }));
        Assert.True(
            effect.CanHandle(
                new SagaStepFailed
                {
                    StepIndex = 0,
                    StepName = "Step",
                    ErrorCode = "ERR",
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
            new(0, "Debit", typeof(SagaNonCompensatableStep), false),
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
        SagaStepCompensated compensated = Assert.IsType<SagaStepCompensated>(Assert.Single(events));
        Assert.Equal(0, compensated.StepIndex);
        Assert.Equal("Debit", compensated.StepName);
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
            new(0, "Debit", typeof(SagaCompensationSuccessStep), true),
            new(1, "Credit", typeof(SagaNonCompensatableStep), false),
        ];
        using ServiceProvider provider = CreateProvider(services =>
        {
            services.AddTransient<SagaCompensationSuccessStep>();
            services.AddTransient<SagaNonCompensatableStep>();
        });
        SagaOrchestrationEffect<TestSagaState> effect = CreateEffect(steps, provider);
        List<object> events = await CollectAsync(
            effect.HandleAsync(
                new SagaStepCompensated
                {
                    StepIndex = 1,
                    StepName = "Credit",
                },
                new(),
                "saga",
                0,
                CancellationToken.None));
        SagaStepCompensated compensated = Assert.IsType<SagaStepCompensated>(Assert.Single(events));
        Assert.Equal(0, compensated.StepIndex);
        Assert.Equal("Debit", compensated.StepName);
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
            new(0, "Debit", typeof(SagaSuccessStep), false),
        ];
        using ServiceProvider provider = CreateProvider(services => services.AddTransient<SagaSuccessStep>());
        SagaOrchestrationEffect<TestSagaState> effect = CreateEffect(steps, provider, timeProvider);
        List<object> events = await CollectAsync(
            effect.HandleAsync(
                new SagaStepCompleted
                {
                    StepIndex = 0,
                    StepName = "Debit",
                    CompletedAt = now,
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
            new(0, "Debit", typeof(SagaFailStep), false),
        ];
        using ServiceProvider provider = CreateProvider(services => services.AddTransient<SagaFailStep>());
        SagaOrchestrationEffect<TestSagaState> effect = CreateEffect(steps, provider);
        DateTimeOffset startedAt = new(2025, 2, 12, 12, 0, 0, TimeSpan.Zero);
        List<object> events = await CollectAsync(
            effect.HandleAsync(
                new SagaStartedEvent
                {
                    SagaId = Guid.NewGuid(),
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
                SagaStepFailed failed = Assert.IsType<SagaStepFailed>(item);
                Assert.Equal("ERR", failed.ErrorCode);
                Assert.Equal("boom", failed.ErrorMessage);
                Assert.Equal(0, failed.StepIndex);
            },
            item =>
            {
                SagaCompensating compensating = Assert.IsType<SagaCompensating>(item);
                Assert.Equal(-1, compensating.FromStepIndex);
            });
    }

    /// <summary>
    ///     Verifies thrown step exceptions emit failure and compensating events.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task HandleAsyncEmitsStepFailedAndCompensatingWhenStepThrows()
    {
        Guid sagaId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        const string correlationId = "transfer-46";
        SagaStepInfo[] steps =
        [
            new(0, "Debit", typeof(SagaThrowingStep), false),
        ];
        using ServiceProvider provider = CreateProvider(services => services.AddTransient<SagaThrowingStep>());
        Mock<ILogger<SagaOrchestrationEffect<TestSagaState>>> loggerMock = new();
        loggerMock.Setup(l => l.IsEnabled(LogLevel.Error)).Returns(true);
        SagaOrchestrationEffect<TestSagaState> effect = CreateEffect(steps, provider, logger: loggerMock.Object);
        List<object> events = await CollectAsync(
            effect.HandleAsync(
                new SagaStartedEvent
                {
                    SagaId = sagaId,
                    StartedAt = new(2025, 2, 12, 12, 30, 0, TimeSpan.Zero),
                    StepHash = "HASH",
                },
                new()
                {
                    SagaId = sagaId,
                    CorrelationId = correlationId,
                },
                "saga",
                0,
                CancellationToken.None));
        Assert.Collection(
            events,
            item =>
            {
                SagaStepFailed failed = Assert.IsType<SagaStepFailed>(item);
                Assert.Equal("SAGA_STEP_EXCEPTION", failed.ErrorCode);
                Assert.Equal("kapow", failed.ErrorMessage);
                Assert.Equal(0, failed.StepIndex);
                Assert.Equal("Debit", failed.StepName);
            },
            item =>
            {
                SagaCompensating compensating = Assert.IsType<SagaCompensating>(item);
                Assert.Equal(-1, compensating.FromStepIndex);
            });
        VerifyExceptionLog(
            loggerMock,
            LogLevel.Error,
            3,
            "kapow",
            "Debit",
            "SAGA_STEP_EXCEPTION",
            sagaId.ToString(),
            correlationId,
            "saga");
        VerifyNoLog(loggerMock, LogLevel.Error, 5);
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
            new(0, "Debit", typeof(SagaSuccessStep), false),
            new(1, "Credit", typeof(SagaSuccessStep), false),
        ];
        using ServiceProvider provider = CreateProvider(services => services.AddTransient<SagaSuccessStep>());
        SagaOrchestrationEffect<TestSagaState> effect = CreateEffect(steps, provider, timeProvider);
        List<object> events = await CollectAsync(
            effect.HandleAsync(
                new SagaStepCompleted
                {
                    StepIndex = 0,
                    StepName = "Debit",
                    CompletedAt = now,
                },
                new(),
                "saga",
                0,
                CancellationToken.None));
        Assert.Collection(
            events,
            item => Assert.IsType<SagaMarkerEvent>(item),
            item =>
            {
                SagaStepCompleted completed = Assert.IsType<SagaStepCompleted>(item);
                Assert.Equal(1, completed.StepIndex);
                Assert.Equal("Credit", completed.StepName);
                Assert.Equal(now, completed.CompletedAt);
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
            new(0, "Debit", typeof(SagaSuccessStep), false),
        ];
        using ServiceProvider provider = CreateProvider(services => services.AddTransient<SagaSuccessStep>());
        SagaOrchestrationEffect<TestSagaState> effect = CreateEffect(steps, provider, timeProvider);
        List<object> events = await CollectAsync(
            effect.HandleAsync(
                new SagaStartedEvent
                {
                    SagaId = Guid.NewGuid(),
                    StartedAt = now,
                    StepHash = "HASH",
                },
                new(),
                "saga",
                0,
                CancellationToken.None));
        Assert.Collection(
            events,
            item => Assert.IsType<SagaMarkerEvent>(item),
            item =>
            {
                SagaStepCompleted completed = Assert.IsType<SagaStepCompleted>(item);
                Assert.Equal(0, completed.StepIndex);
                Assert.Equal("Debit", completed.StepName);
                Assert.Equal(now, completed.CompletedAt);
            });
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
            new(0, "Debit", typeof(SagaCompensationFailStep), true),
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
        SagaFailed failed = Assert.IsType<SagaFailed>(Assert.Single(events));
        Assert.Equal("FAIL", failed.ErrorCode);
        Assert.Equal("nope", failed.ErrorMessage);
    }

    /// <summary>
    ///     Verifies thrown compensation exceptions emit a terminal saga failure.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task HandleAsyncFailsWhenCompensationThrows()
    {
        Guid sagaId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        const string correlationId = "transfer-47";
        SagaStepInfo[] steps =
        [
            new(0, "Debit", typeof(SagaCompensationThrowingStep), true),
        ];
        using ServiceProvider provider = CreateProvider(services =>
            services.AddTransient<SagaCompensationThrowingStep>());
        Mock<ILogger<SagaOrchestrationEffect<TestSagaState>>> loggerMock = new();
        loggerMock.Setup(l => l.IsEnabled(LogLevel.Error)).Returns(true);
        SagaOrchestrationEffect<TestSagaState> effect = CreateEffect(steps, provider, logger: loggerMock.Object);
        List<object> events = await CollectAsync(
            effect.HandleAsync(
                new SagaCompensating
                {
                    FromStepIndex = 0,
                },
                new()
                {
                    SagaId = sagaId,
                    CorrelationId = correlationId,
                },
                "saga",
                0,
                CancellationToken.None));
        SagaFailed failed = Assert.IsType<SagaFailed>(Assert.Single(events));
        Assert.Equal("COMPENSATION_EXCEPTION", failed.ErrorCode);
        Assert.Equal("compensation blew up", failed.ErrorMessage);
        VerifyExceptionLog(
            loggerMock,
            LogLevel.Error,
            4,
            "compensation blew up",
            "Debit",
            "COMPENSATION_EXCEPTION",
            sagaId.ToString(),
            correlationId,
            "saga");
        VerifyNoLog(loggerMock, LogLevel.Error, 6);
    }

    /// <summary>
    ///     Verifies compensation failures are logged with structured fields for observability.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task HandleAsyncLogsCompensationFailureWithStructuredFields()
    {
        Guid sagaId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        const string correlationId = "transfer-43";
        SagaStepInfo[] steps =
        [
            new(0, "Debit", typeof(SagaCompensationFailStep), true),
        ];
        using ServiceProvider provider = CreateProvider(services => services.AddTransient<SagaCompensationFailStep>());
        Mock<ILogger<SagaOrchestrationEffect<TestSagaState>>> loggerMock = new();
        loggerMock.Setup(l => l.IsEnabled(LogLevel.Error)).Returns(true);
        SagaOrchestrationEffect<TestSagaState> effect = CreateEffect(steps, provider, logger: loggerMock.Object);
        List<object> events = await CollectAsync(
            effect.HandleAsync(
                new SagaCompensating
                {
                    FromStepIndex = 0,
                },
                new()
                {
                    SagaId = sagaId,
                    CorrelationId = correlationId,
                },
                "saga",
                0,
                CancellationToken.None));
        SagaFailed failed = Assert.IsType<SagaFailed>(Assert.Single(events));
        Assert.Equal("FAIL", failed.ErrorCode);
        VerifyLog(loggerMock, LogLevel.Error, 6, "Debit", "FAIL", "nope", sagaId.ToString(), correlationId, "saga");
    }

    /// <summary>
    ///     Verifies missing compensation metadata is logged with structured fields for observability.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task HandleAsyncLogsMissingCompensationMetadataWithStructuredFields()
    {
        Guid sagaId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        const string correlationId = "transfer-44";
        using ServiceProvider provider = CreateProvider();
        Mock<ILogger<SagaOrchestrationEffect<TestSagaState>>> loggerMock = new();
        loggerMock.Setup(l => l.IsEnabled(LogLevel.Error)).Returns(true);
        SagaOrchestrationEffect<TestSagaState> effect = CreateEffect(
            Array.Empty<SagaStepInfo>(),
            provider,
            logger: loggerMock.Object);
        List<object> events = await CollectAsync(
            effect.HandleAsync(
                new SagaCompensating
                {
                    FromStepIndex = 0,
                },
                new()
                {
                    SagaId = sagaId,
                    CorrelationId = correlationId,
                },
                "saga",
                0,
                CancellationToken.None));
        SagaFailed failed = Assert.IsType<SagaFailed>(Assert.Single(events));
        Assert.Equal("COMPENSATION_FAILED", failed.ErrorCode);
        VerifyLog(
            loggerMock,
            LogLevel.Error,
            8,
            "COMPENSATION_FAILED",
            "Step metadata not found.",
            sagaId.ToString(),
            correlationId,
            "saga");
    }

    /// <summary>
    ///     Verifies missing step metadata is logged with structured fields for observability.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task HandleAsyncLogsMissingStepMetadataWithStructuredFields()
    {
        Guid sagaId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        const string correlationId = "transfer-45";
        using ServiceProvider provider = CreateProvider();
        Mock<ILogger<SagaOrchestrationEffect<TestSagaState>>> loggerMock = new();
        loggerMock.Setup(l => l.IsEnabled(LogLevel.Error)).Returns(true);
        SagaOrchestrationEffect<TestSagaState> effect = CreateEffect(
            Array.Empty<SagaStepInfo>(),
            provider,
            logger: loggerMock.Object);
        List<object> events = await CollectAsync(
            effect.HandleAsync(
                new SagaStartedEvent
                {
                    SagaId = sagaId,
                    StartedAt = new(2025, 2, 12, 14, 0, 0, TimeSpan.Zero),
                    StepHash = "HASH",
                },
                new()
                {
                    SagaId = sagaId,
                    CorrelationId = correlationId,
                },
                "saga",
                0,
                CancellationToken.None));
        SagaFailed failure = Assert.IsType<SagaFailed>(Assert.Single(events));
        Assert.Equal("STEP_METADATA_MISSING", failure.ErrorCode);
        VerifyLog(
            loggerMock,
            LogLevel.Error,
            7,
            "STEP_METADATA_MISSING",
            "Step metadata not found.",
            sagaId.ToString(),
            correlationId,
            "saga");
    }

    /// <summary>
    ///     Verifies step failures are logged with structured fields for observability.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task HandleAsyncLogsStepFailureWithStructuredFields()
    {
        Guid sagaId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        const string correlationId = "transfer-42";
        SagaStepInfo[] steps =
        [
            new(0, "Debit", typeof(SagaFailStep), false),
        ];
        using ServiceProvider provider = CreateProvider(services => services.AddTransient<SagaFailStep>());
        Mock<ILogger<SagaOrchestrationEffect<TestSagaState>>> loggerMock = new();
        loggerMock.Setup(l => l.IsEnabled(LogLevel.Error)).Returns(true);
        SagaOrchestrationEffect<TestSagaState> effect = CreateEffect(steps, provider, logger: loggerMock.Object);
        TestSagaState state = new()
        {
            SagaId = sagaId,
            CorrelationId = correlationId,
        };
        List<object> events = await CollectAsync(
            effect.HandleAsync(
                new SagaStartedEvent
                {
                    SagaId = sagaId,
                    StartedAt = new(2025, 2, 12, 12, 0, 0, TimeSpan.Zero),
                    StepHash = "HASH",
                },
                state,
                "saga",
                0,
                CancellationToken.None));
        Assert.Equal(2, events.Count);
        VerifyLog(loggerMock, LogLevel.Error, 5, "Debit", "ERR", "boom", sagaId.ToString(), correlationId, "saga");
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
                    StepIndex = 0,
                    StepName = "Step",
                    ErrorCode = "ERR",
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
            new(0, "Invalid", typeof(SagaInvalidStep), false),
        ];
        using ServiceProvider provider = CreateProvider(services => services.AddTransient<SagaInvalidStep>());
        SagaOrchestrationEffect<TestSagaState> effect = CreateEffect(steps, provider);
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (object item in effect.HandleAsync(
                               new SagaStartedEvent
                               {
                                   SagaId = Guid.NewGuid(),
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
            new(0, "Debit", typeof(SagaCompensationSkippedStep), true),
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
        SagaStepCompensated compensated = Assert.IsType<SagaStepCompensated>(Assert.Single(events));
        Assert.Equal(0, compensated.StepIndex);
        Assert.Equal("Debit", compensated.StepName);
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
            new(0, "Debit", typeof(SagaCompensationNoCodeStep), true),
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
        SagaFailed failed = Assert.IsType<SagaFailed>(Assert.Single(events));
        Assert.Equal("COMPENSATION_FAILED", failed.ErrorCode);
        Assert.Equal("nope", failed.ErrorMessage);
    }
}