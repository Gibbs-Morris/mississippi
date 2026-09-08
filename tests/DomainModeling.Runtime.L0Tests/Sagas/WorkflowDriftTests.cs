using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

using Mississippi.DomainModeling.Abstractions;
using Mississippi.DomainModeling.Runtime.Sagas;

using Moq;


namespace Mississippi.DomainModeling.Runtime.L0Tests.Sagas;

/// <summary>
///     Verifies that persisted workflow definitions protect forward and compensation execution.
/// </summary>
public sealed class WorkflowDriftTests
{
    /// <summary>
    ///     Gets the combinations of replay boundaries and incompatible workflow changes.
    /// </summary>
    public static TheoryData<string, string> ChangedWorkflows
    {
        get
        {
            TheoryData<string, string> cases = new();
            foreach (string boundary in new[] { "Started", "Completed", "Compensating", "Compensated" })
            {
                foreach (string change in new[]
                         {
                             "Name", "Type", "Index", "Compensation", "Order", "Removed", "InvalidUnicode",
                         })
                {
                    cases.Add(boundary, change);
                }
            }

            return cases;
        }
    }

    private static object CreateBoundary(
        string boundary,
        SagaStartedEvent started
    ) =>
        boundary switch
        {
            "Started" => started,
            "Completed" => new SagaStepCompleted
            {
                StepIndex = 0,
                StepName = "Debit",
                CompletedAt = started.StartedAt,
            },
            "Compensating" => new SagaCompensating
            {
                FromStepIndex = 1,
            },
            "Compensated" => new SagaStepCompensated
            {
                StepIndex = 1,
                StepName = "Credit",
            },
            var _ => throw new ArgumentOutOfRangeException(nameof(boundary)),
        };

    private static SagaStepInfo[] CreateChangedSteps(
        string change
    ) =>
        change switch
        {
            "Name" => [new(0, "ChangedDebit", typeof(SagaSuccessStep), true), CreateSteps()[1]],
            "Type" => [new(0, "Debit", typeof(SagaCompensationSuccessStep), true), CreateSteps()[1]],
            "Index" => [new(2, "Debit", typeof(SagaSuccessStep), true), CreateSteps()[1]],
            "Compensation" => [new(0, "Debit", typeof(SagaSuccessStep), false), CreateSteps()[1]],
            "Order" => CreateSteps().Reverse().ToArray(),
            "Removed" => [CreateSteps()[0]],
            "InvalidUnicode" => [new(0, new((char)0xD800, 1), typeof(SagaSuccessStep), true), CreateSteps()[1]],
            var _ => throw new ArgumentOutOfRangeException(nameof(change)),
        };

    private static SagaStartedEvent CreateStartedEvent(
        FakeTimeProvider timeProvider
    )
    {
        StartSagaCommandHandler<TestSagaState, string> handler = new(
            new SagaStepInfoProvider<TestSagaState>(CreateSteps()),
            timeProvider);
        OperationResult<IReadOnlyList<object>> result = handler.Handle(
            new()
            {
                SagaId = Guid.Parse("af5874e5-5990-4b17-902d-818ee15a6869"),
                Input = "transfer",
            },
            null);
        Assert.True(result.Success);
        return Assert.IsType<SagaStartedEvent>(result.Value[0]);
    }

    private static Type CreateStepType(
        string assemblyName,
        int version
    )
    {
        AssemblyName name = new(assemblyName)
        {
            Version = new(version, 0),
        };
        AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.RunAndCollect);
        return assembly.DefineDynamicModule(assemblyName).DefineType("Workflow.Step").CreateType();
    }

    private static SagaStepInfo[] CreateSteps() =>
    [
        new(0, "Debit", typeof(SagaSuccessStep), true),
        new(1, "Credit", typeof(SagaCompensationSuccessStep), true),
    ];

    /// <summary>
    ///     Verifies deployment changes stop orchestration before resolving or invoking any step.
    /// </summary>
    /// <param name="boundary">The persisted boundary being replayed.</param>
    /// <param name="change">The incompatible metadata change.</param>
    /// <returns>A task representing the test.</returns>
    [Theory]
    [MemberData(nameof(ChangedWorkflows))]
    public async Task ChangedWorkflowStopsBeforeExecutingStep(
        string boundary,
        string change
    )
    {
        FakeTimeProvider timeProvider = new(new(2026, 9, 8, 12, 0, 0, TimeSpan.Zero));
        SagaStartedEvent started = CreateStartedEvent(timeProvider);
        TestSagaState state = new()
        {
            SagaId = started.SagaId,
            StepHash = started.StepHash,
            Phase = boundary is "Compensating" or "Compensated" ? SagaPhase.Compensating : SagaPhase.Running,
        };
        Mock<ISagaStep<TestSagaState>> step = new();
        step.Setup(s => s.ExecuteAsync(It.IsAny<TestSagaState>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(StepResult.Succeeded());
        step.As<ICompensatable<TestSagaState>>()
            .Setup(s => s.CompensateAsync(It.IsAny<TestSagaState>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CompensationResult.Succeeded());
        Mock<IServiceProvider> services = new();
        services.Setup(s => s.GetService(It.IsAny<Type>())).Returns(step.Object);
        Mock<ILogger<SagaOrchestrationEffect<TestSagaState>>> logger = new();
        logger.Setup(l => l.IsEnabled(LogLevel.Error)).Returns(true);
        SagaOrchestrationEffect<TestSagaState> effect = new(
            new SagaStepInfoProvider<TestSagaState>(CreateChangedSteps(change)),
            services.Object,
            timeProvider,
            logger.Object);
        List<object> events = await effect.HandleAsync(
                CreateBoundary(boundary, started),
                state,
                "transfer",
                10,
                CancellationToken.None)
            .ToListAsync();
        SagaFailed failure = Assert.IsType<SagaFailed>(Assert.Single(events));
        Assert.Equal("SAGA_STEP_HASH_MISMATCH", failure.ErrorCode);
        Assert.Equal(
            "The registered saga steps differ from the persisted workflow definition or cannot be hashed.",
            failure.ErrorMessage);
        Assert.Equal(timeProvider.GetUtcNow(), failure.FailedAt);
        Assert.Equal(SagaPhase.Failed, new SagaFailedReducer<TestSagaState>().Reduce(state, failure).Phase);
        services.Verify(s => s.GetService(It.IsAny<Type>()), Times.Never);
        step.Verify(s => s.ExecuteAsync(It.IsAny<TestSagaState>(), It.IsAny<CancellationToken>()), Times.Never);
        step.As<ICompensatable<TestSagaState>>()
            .Verify(s => s.CompensateAsync(It.IsAny<TestSagaState>(), It.IsAny<CancellationToken>()), Times.Never);
        if (change == "InvalidUnicode")
        {
            logger.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.Is<EventId>(id => id.Id == 5),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<EncoderFallbackException>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }

    /// <summary>
    ///     Verifies catastrophic metadata failures propagate instead of being converted into workflow drift.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task CriticalMetadataFailurePropagates()
    {
        FakeTimeProvider timeProvider = new();
        SagaStartedEvent started = CreateStartedEvent(timeProvider);
        Mock<ISagaStepInfoProvider<TestSagaState>> metadata = new();
        metadata.SetupGet(p => p.Steps).Throws(new ThreadInterruptedException());
        SagaOrchestrationEffect<TestSagaState> effect = new(metadata.Object, Mock.Of<IServiceProvider>(), timeProvider);
        await Assert.ThrowsAsync<ThreadInterruptedException>(() => effect.HandleAsync(
                started,
                new()
                {
                    StepHash = started.StepHash,
                },
                "transfer",
                0,
                CancellationToken.None)
            .ToListAsync()
            .AsTask());
    }

    /// <summary>
    ///     Verifies missing persisted workflow identity is not permission to execute registered steps.
    /// </summary>
    /// <param name="stepHash">The invalid persisted workflow hash.</param>
    /// <returns>A task representing the test.</returns>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid")]
    public async Task MissingWorkflowHashStopsBeforeResolvingStep(
        string? stepHash
    )
    {
        FakeTimeProvider timeProvider = new();
        SagaStartedEvent started = CreateStartedEvent(timeProvider);
        Mock<IServiceProvider> services = new(MockBehavior.Strict);
        Mock<ILogger<SagaOrchestrationEffect<TestSagaState>>> logger = new();
        logger.Setup(l => l.IsEnabled(LogLevel.Error)).Returns(true);
        SagaOrchestrationEffect<TestSagaState> effect = new(
            new SagaStepInfoProvider<TestSagaState>(CreateSteps()),
            services.Object,
            timeProvider,
            logger.Object);
        List<object> events = await effect.HandleAsync(
                started,
                new()
                {
                    StepHash = stepHash,
                },
                "transfer",
                0,
                CancellationToken.None)
            .ToListAsync();
        Assert.Equal("SAGA_STEP_HASH_MISMATCH", Assert.IsType<SagaFailed>(Assert.Single(events)).ErrorCode);
        services.VerifyNoOtherCalls();
        logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.Is<EventId>(id => id.Id == 5),
                It.Is<It.IsAnyType>((value, type) => value.ToString()!.Contains("transfer", StringComparison.Ordinal)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    ///     Verifies invalid arguments fail when orchestration is called rather than when its stream is enumerated.
    /// </summary>
    [Fact]
    public void OrchestrationValidatesArgumentsBeforeEnumeration()
    {
        SagaOrchestrationEffect<TestSagaState> effect = new(
            new SagaStepInfoProvider<TestSagaState>(CreateSteps()),
            Mock.Of<IServiceProvider>(),
            new FakeTimeProvider());
        ArgumentNullException eventError = Assert.Throws<ArgumentNullException>(() =>
            effect.HandleAsync(null!, new(), "transfer", 0, CancellationToken.None));
        ArgumentNullException stateError = Assert.Throws<ArgumentNullException>(() =>
            effect.HandleAsync(new(), null!, "transfer", 0, CancellationToken.None));
        Assert.Equal("eventData", eventError.ParamName);
        Assert.Equal("currentState", stateError.ParamName);
    }

    /// <summary>
    ///     Verifies execution uses the validated metadata even when re-reading its provider would change the shared list.
    /// </summary>
    /// <param name="boundary">The persisted boundary being replayed.</param>
    /// <returns>A task representing the test.</returns>
    [Theory]
    [InlineData("Started")]
    [InlineData("Completed")]
    [InlineData("Compensating")]
    [InlineData("Compensated")]
    public async Task ValidatedWorkflowUsesSingleMetadataSnapshot(
        string boundary
    )
    {
        FakeTimeProvider timeProvider = new();
        SagaStartedEvent started = CreateStartedEvent(timeProvider);
        TestSagaState state = new SagaStartedReducer<TestSagaState>().Reduce(new(), started);
        List<SagaStepInfo> steps = CreateSteps().ToList();
        int reads = 0;
        Mock<ISagaStepInfoProvider<TestSagaState>> metadata = new();
        metadata.SetupGet(p => p.Steps)
            .Returns(() =>
            {
                if (reads++ > 0)
                {
                    steps[0] = new(0, "ChangedDebit", typeof(SagaFailStep), false);
                    steps[1] = new(1, "ChangedCredit", typeof(SagaFailStep), false);
                }

                return steps;
            });
        ServiceCollection services = new();
        services.AddTransient<SagaSuccessStep>();
        services.AddTransient<SagaCompensationSuccessStep>();
        services.AddTransient<SagaFailStep>();
        using ServiceProvider provider = services.BuildServiceProvider();
        SagaOrchestrationEffect<TestSagaState> effect = new(metadata.Object, provider, timeProvider);
        List<object> events = await effect.HandleAsync(
                CreateBoundary(boundary, started),
                state,
                "transfer",
                10,
                CancellationToken.None)
            .ToListAsync();
        if (boundary is "Compensating" or "Compensated")
        {
            SagaStepCompensated compensated = Assert.IsType<SagaStepCompensated>(Assert.Single(events));
            Assert.Equal(boundary == "Compensating" ? 1 : 0, compensated.StepIndex);
            Assert.Equal(boundary == "Compensating" ? "Credit" : "Debit", compensated.StepName);
        }
        else
        {
            SagaStepCompleted completed = Assert.IsType<SagaStepCompleted>(events[^1]);
            Assert.Equal(boundary == "Started" ? 0 : 1, completed.StepIndex);
            Assert.Equal(boundary == "Started" ? 2 : 1, events.Count);
        }

        metadata.VerifyGet(p => p.Steps, Times.Once);
    }

    /// <summary>
    ///     Verifies delimiter characters in a step name cannot disguise a removed step.
    /// </summary>
    [Fact]
    public void WorkflowHashCannotHideStepInName()
    {
        Type stepType = typeof(SagaSuccessStep);
        SagaStepInfo first = new(0, "A", stepType, true);
        SagaStepInfo second = new(1, "B", stepType, true);
        SagaStepInfo combined = new(0, $"A:{stepType.FullName}:True|1:B", stepType, true);
        Assert.NotEqual(SagaStepHash.Compute([first, second]), SagaStepHash.Compute([combined]));
        Assert.Throws<EncoderFallbackException>(() =>
            SagaStepHash.Compute([new(0, new((char)0xD800, 1), stepType, true)]));
    }

    /// <summary>
    ///     Verifies the shared hash supports types without a full name through Orleans type formatting.
    /// </summary>
    [Fact]
    public void WorkflowHashSupportsTypeWithoutFullName()
    {
        Type typeParameter = typeof(List<>).GetGenericArguments()[0];
        Assert.Null(typeParameter.FullName);
        Assert.Equal(
            "ED0618B6485232FA73644E714144718AD2EAB28DE39C12162FC6FB77559F99CF",
            SagaStepHash.Compute([new(0, "Open", typeParameter, false)]));
        Assert.Equal("steps", Assert.Throws<ArgumentNullException>(() => SagaStepHash.Compute(null!)).ParamName);
    }

    /// <summary>
    ///     Verifies assembly identity distinguishes types without treating assembly versions as workflow changes.
    /// </summary>
    /// <param name="assemblyName">The replacement assembly name.</param>
    /// <param name="isGeneric">Whether the type is used as a generic argument.</param>
    /// <param name="shouldMatch">Whether both definitions have the same stable identity.</param>
    [Theory]
    [InlineData("Original", false, true)]
    [InlineData("Replacement", false, false)]
    [InlineData("Original", true, true)]
    [InlineData("Replacement", true, false)]
    public void WorkflowHashUsesStableAssemblyIdentity(
        string assemblyName,
        bool isGeneric,
        bool shouldMatch
    )
    {
        Type original = CreateStepType("Original", 1);
        Type replacement = CreateStepType(assemblyName, 2);
        if (isGeneric)
        {
            original = typeof(List<>).MakeGenericType(original);
            replacement = typeof(List<>).MakeGenericType(replacement);
        }

        string originalHash = SagaStepHash.Compute([new(0, "Step", original, true)]);
        string replacementHash = SagaStepHash.Compute([new(0, "Step", replacement, true)]);
        Assert.Equal(shouldMatch, originalHash == replacementHash);
    }
}