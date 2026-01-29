using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.EventSourcing.Sagas.Abstractions;

using Moq;


namespace Mississippi.EventSourcing.Sagas.L0Tests;

/// <summary>
///     Tests for <see cref="SagaStepRegistry{TSaga}" />.
/// </summary>
public sealed class SagaStepRegistryTests
{
    /// <summary>
    ///     Test step 1 for the test saga.
    /// </summary>
    [SagaStep(1)]
    private sealed class Step1 : SagaStepBase<TestSagaWithSteps>
    {
        public override Task<StepResult> ExecuteAsync(
            ISagaContext context,
            TestSagaWithSteps state,
            CancellationToken cancellationToken
        ) =>
            Task.FromResult(StepResult.Succeeded());
    }

    /// <summary>
    ///     Test compensation for Step1.
    /// </summary>
    [SagaCompensation(typeof(Step1))]
    private sealed class Step1Compensation : SagaCompensationBase<TestSagaWithSteps>
    {
        public override Task<CompensationResult> CompensateAsync(
            ISagaContext context,
            TestSagaWithSteps state,
            CancellationToken cancellationToken
        ) =>
            Task.FromResult(CompensationResult.Succeeded());
    }

    /// <summary>
    ///     Test step 2 for the test saga with timeout.
    /// </summary>
    [SagaStep(2, Timeout = "00:05:00")]
    private sealed class Step2 : SagaStepBase<TestSagaWithSteps>
    {
        public override Task<StepResult> ExecuteAsync(
            ISagaContext context,
            TestSagaWithSteps state,
            CancellationToken cancellationToken
        ) =>
            Task.FromResult(StepResult.Succeeded());
    }

    /// <summary>
    ///     Test saga state with defined steps.
    /// </summary>
    private sealed record TestSagaWithSteps : ISagaState
    {
        public string? CorrelationId { get; init; }

        public int CurrentStepAttempt { get; init; } = 1;

        public int LastCompletedStepIndex { get; init; } = -1;

        public SagaPhase Phase { get; init; } = SagaPhase.NotStarted;

        public Guid SagaId { get; init; }

        public DateTimeOffset? StartedAt { get; init; }

        public string? StepHash { get; init; }

        public ISagaState ApplyPhase(
            SagaPhase phase
        ) =>
            this with
            {
                Phase = phase,
            };

        public ISagaState ApplySagaStarted(
            Guid sagaId,
            string? correlationId,
            string? stepHash,
            DateTimeOffset startedAt
        ) =>
            this with
            {
                SagaId = sagaId,
                CorrelationId = correlationId,
                StepHash = stepHash,
                StartedAt = startedAt,
                Phase = SagaPhase.Running,
            };

        public ISagaState ApplyStepProgress(
            int lastCompletedStepIndex,
            int currentStepAttempt
        ) =>
            this with
            {
                LastCompletedStepIndex = lastCompletedStepIndex,
                CurrentStepAttempt = currentStepAttempt,
            };
    }

    /// <summary>
    ///     Test saga state without defined steps.
    /// </summary>
    private sealed record TestSagaWithoutSteps : ISagaState
    {
        public string? CorrelationId { get; init; }

        public int CurrentStepAttempt { get; init; } = 1;

        public int LastCompletedStepIndex { get; init; } = -1;

        public SagaPhase Phase { get; init; } = SagaPhase.NotStarted;

        public Guid SagaId { get; init; }

        public DateTimeOffset? StartedAt { get; init; }

        public string? StepHash { get; init; }

        public ISagaState ApplyPhase(
            SagaPhase phase
        ) =>
            this with
            {
                Phase = phase,
            };

        public ISagaState ApplySagaStarted(
            Guid sagaId,
            string? correlationId,
            string? stepHash,
            DateTimeOffset startedAt
        ) =>
            this with
            {
                SagaId = sagaId,
                CorrelationId = correlationId,
                StepHash = stepHash,
                StartedAt = startedAt,
                Phase = SagaPhase.Running,
            };

        public ISagaState ApplyStepProgress(
            int lastCompletedStepIndex,
            int currentStepAttempt
        ) =>
            this with
            {
                LastCompletedStepIndex = lastCompletedStepIndex,
                CurrentStepAttempt = currentStepAttempt,
            };
    }

    /// <summary>
    ///     Registry for saga with no steps returns empty list.
    /// </summary>
    [Fact]
    public void RegistryForSagaWithNoStepsShouldReturnEmptyList()
    {
        // Arrange
        Mock<IServiceProvider> serviceProviderMock = new();
        SagaStepRegistry<TestSagaWithoutSteps> sut = new(serviceProviderMock.Object);

        // Act
        IReadOnlyList<ISagaStepInfo> steps = sut.Steps;

        // Assert
        Assert.Empty(steps);
    }

    /// <summary>
    ///     StepHash for saga with no steps is still computed.
    /// </summary>
    [Fact]
    public void StepHashForSagaWithNoStepsShouldBeComputed()
    {
        // Arrange
        Mock<IServiceProvider> serviceProviderMock = new();
        SagaStepRegistry<TestSagaWithoutSteps> sut = new(serviceProviderMock.Object);

        // Act
        string hash = sut.StepHash;

        // Assert
        Assert.NotEmpty(hash);
    }

    /// <summary>
    ///     StepHash property is computed and consistent.
    /// </summary>
    [Fact]
    public void StepHashShouldBeComputedAndConsistent()
    {
        // Arrange
        Mock<IServiceProvider> serviceProviderMock = new();
        SagaStepRegistry<TestSagaWithSteps> sut = new(serviceProviderMock.Object);

        // Act
        string hash1 = sut.StepHash;
        string hash2 = sut.StepHash;

        // Assert
        Assert.NotEmpty(hash1);
        Assert.Equal(hash1, hash2);
    }

    /// <summary>
    ///     Step with compensation is linked correctly.
    /// </summary>
    [Fact]
    public void StepWithCompensationShouldBeLinked()
    {
        // Arrange
        Mock<IServiceProvider> serviceProviderMock = new();
        SagaStepRegistry<TestSagaWithSteps> sut = new(serviceProviderMock.Object);

        // Act
        IReadOnlyList<ISagaStepInfo> steps = sut.Steps;

        // Assert
        ISagaStepInfo? stepWithCompensation = steps[0];
        Assert.NotNull(stepWithCompensation.CompensationType);
        Assert.Equal(typeof(Step1Compensation), stepWithCompensation.CompensationType);
    }

    /// <summary>
    ///     Step with timeout is parsed correctly.
    /// </summary>
    [Fact]
    public void StepWithTimeoutShouldBeParsed()
    {
        // Arrange
        Mock<IServiceProvider> serviceProviderMock = new();
        SagaStepRegistry<TestSagaWithSteps> sut = new(serviceProviderMock.Object);

        // Act
        IReadOnlyList<ISagaStepInfo> steps = sut.Steps;

        // Assert
        ISagaStepInfo? stepWithTimeout = steps.Count > 1 ? steps[1] : null;
        Assert.NotNull(stepWithTimeout);
        Assert.Equal(TimeSpan.FromMinutes(5), stepWithTimeout.Timeout);
    }

    /// <summary>
    ///     Steps without compensation return null CompensationType.
    /// </summary>
    [Fact]
    public void StepWithoutCompensationShouldReturnNullCompensationType()
    {
        // Arrange
        Mock<IServiceProvider> serviceProviderMock = new();
        SagaStepRegistry<TestSagaWithSteps> sut = new(serviceProviderMock.Object);

        // Act
        IReadOnlyList<ISagaStepInfo> steps = sut.Steps;

        // Assert
        ISagaStepInfo? stepWithoutCompensation = steps.Count > 1 ? steps[1] : null;
        Assert.NotNull(stepWithoutCompensation);
        Assert.Null(stepWithoutCompensation.CompensationType);
    }

    /// <summary>
    ///     Steps without timeout return null.
    /// </summary>
    [Fact]
    public void StepWithoutTimeoutShouldReturnNull()
    {
        // Arrange
        Mock<IServiceProvider> serviceProviderMock = new();
        SagaStepRegistry<TestSagaWithSteps> sut = new(serviceProviderMock.Object);

        // Act
        IReadOnlyList<ISagaStepInfo> steps = sut.Steps;

        // Assert
        ISagaStepInfo? stepWithoutTimeout = steps[0];
        Assert.Null(stepWithoutTimeout.Timeout);
    }

    /// <summary>
    ///     Steps are cached after first access.
    /// </summary>
    [Fact]
    public void StepsShouldBeCached()
    {
        // Arrange
        Mock<IServiceProvider> serviceProviderMock = new();
        SagaStepRegistry<TestSagaWithSteps> sut = new(serviceProviderMock.Object);

        // Act
        IReadOnlyList<ISagaStepInfo> steps1 = sut.Steps;
        IReadOnlyList<ISagaStepInfo> steps2 = sut.Steps;

        // Assert
        Assert.Same(steps1, steps2);
    }

    /// <summary>
    ///     Steps property discovers steps from the saga type's assembly.
    /// </summary>
    [Fact]
    public void StepsShouldDiscoverStepsFromAssembly()
    {
        // Arrange
        Mock<IServiceProvider> serviceProviderMock = new();
        SagaStepRegistry<TestSagaWithSteps> sut = new(serviceProviderMock.Object);

        // Act
        IReadOnlyList<ISagaStepInfo> steps = sut.Steps;

        // Assert
        Assert.NotEmpty(steps);
        Assert.Equal(2, steps.Count);
        Assert.Equal("Step1", steps[0].Name);
        Assert.Equal(1, steps[0].Order);
        Assert.Equal("Step2", steps[1].Name);
        Assert.Equal(2, steps[1].Order);
    }

    /// <summary>
    ///     Steps property returns steps in order.
    /// </summary>
    [Fact]
    public void StepsShouldReturnStepsInOrder()
    {
        // Arrange
        Mock<IServiceProvider> serviceProviderMock = new();
        SagaStepRegistry<TestSagaWithSteps> sut = new(serviceProviderMock.Object);

        // Act
        IReadOnlyList<ISagaStepInfo> steps = sut.Steps;

        // Assert
        for (int i = 1; i < steps.Count; i++)
        {
            Assert.True(steps[i].Order > steps[i - 1].Order);
        }
    }
}