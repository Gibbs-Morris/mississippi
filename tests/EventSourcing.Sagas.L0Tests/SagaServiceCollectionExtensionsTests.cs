using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions.Commands;

using Moq;


namespace Mississippi.EventSourcing.Sagas.L0Tests;

/// <summary>
///     Tests for <see cref="SagaServiceCollectionExtensions" />.
/// </summary>
public sealed class SagaServiceCollectionExtensionsTests
{
    /// <summary>
    ///     Test saga definition implementing ISagaDefinition.
    /// </summary>
    private sealed record TestSagaDefinition
        : ISagaDefinition,
          ISagaState
    {
        public static string SagaName => "TestSaga";

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
    ///     Test input for saga.
    /// </summary>
    private sealed record TestSagaInput(string OrderId);

    /// <summary>
    ///     Test step for the saga definition.
    /// </summary>
    [SagaStep(1)]
#pragma warning disable CA1812 // Instantiated via reflection
    private sealed class TestStep : SagaStepBase<TestSagaDefinition>
#pragma warning restore CA1812
    {
        public override Task<StepResult> ExecuteAsync(
            ISagaContext context,
            TestSagaDefinition state,
            CancellationToken cancellationToken
        ) =>
            Task.FromResult(StepResult.Succeeded());
    }

    /// <summary>
    ///     Multiple calls to AddSaga don't duplicate registrations.
    /// </summary>
    [Fact]
    public void AddSagaMultipleTimesShouldNotDuplicateRegistrations()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddSaga<TestSagaDefinition>();
        services.AddSaga<TestSagaDefinition>();

        // Assert
        int registryCount = 0;
        foreach (ServiceDescriptor descriptor in services)
        {
            if (descriptor.ServiceType == typeof(ISagaStepRegistry<TestSagaDefinition>))
            {
                registryCount++;
            }
        }

        Assert.Equal(1, registryCount);
    }

    /// <summary>
    ///     AddSagaOrchestration registers ISagaOrchestrator.
    /// </summary>
    [Fact]
    public void AddSagaOrchestrationShouldRegisterOrchestrator()
    {
        // Arrange
        ServiceCollection services = new();

        // Add required dependency for SagaOrchestrator
        services.AddSingleton<IAggregateGrainFactory>(new Mock<IAggregateGrainFactory>().Object);

        // Act
        services.AddSagaOrchestration();

        // Assert
        using ServiceProvider provider = services.BuildServiceProvider();
        ISagaOrchestrator? orchestrator = provider.GetService<ISagaOrchestrator>();
        Assert.NotNull(orchestrator);
    }

    /// <summary>
    ///     AddSagaOrchestration returns the service collection for chaining.
    /// </summary>
    [Fact]
    public void AddSagaOrchestrationShouldReturnServiceCollectionForChaining()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        IServiceCollection result = services.AddSagaOrchestration();

        // Assert
        Assert.Same(services, result);
    }

    /// <summary>
    ///     AddSaga registers saga event effects.
    /// </summary>
    [Fact]
    public void AddSagaShouldRegisterEventEffects()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddSingleton(TimeProvider.System);
        services.AddLogging();

        // Act
        services.AddSaga<TestSagaDefinition>();

        // Assert
        using ServiceProvider provider = services.BuildServiceProvider();
        IEnumerable<IEventEffect<TestSagaDefinition>> effects =
            provider.GetServices<IEventEffect<TestSagaDefinition>>();
        Assert.NotEmpty(effects);
    }

    /// <summary>
    ///     AddSaga registers ISagaStepRegistry for the saga type.
    /// </summary>
    [Fact]
    public void AddSagaShouldRegisterStepRegistry()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddSaga<TestSagaDefinition>();

        // Assert
        using ServiceProvider provider = services.BuildServiceProvider();
        ISagaStepRegistry<TestSagaDefinition>? registry = provider.GetService<ISagaStepRegistry<TestSagaDefinition>>();
        Assert.NotNull(registry);
    }

    /// <summary>
    ///     AddSaga returns the service collection for chaining.
    /// </summary>
    [Fact]
    public void AddSagaShouldReturnServiceCollectionForChaining()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        IServiceCollection result = services.AddSaga<TestSagaDefinition>();

        // Assert
        Assert.Same(services, result);
    }

    /// <summary>
    ///     AddSaga with input type also registers step registry.
    /// </summary>
    [Fact]
    public void AddSagaWithInputTypeShouldAlsoRegisterStepRegistry()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddSingleton(TimeProvider.System);

        // Act
        services.AddSaga<TestSagaDefinition, TestSagaInput>();

        // Assert
        using ServiceProvider provider = services.BuildServiceProvider();
        ISagaStepRegistry<TestSagaDefinition>? registry = provider.GetService<ISagaStepRegistry<TestSagaDefinition>>();
        Assert.NotNull(registry);
    }

    /// <summary>
    ///     AddSaga with input type registers StartSagaCommandHandler.
    /// </summary>
    [Fact]
    public void AddSagaWithInputTypeShouldRegisterCommandHandler()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddSingleton(TimeProvider.System);

        // Act
        services.AddSaga<TestSagaDefinition, TestSagaInput>();

        // Assert
        using ServiceProvider provider = services.BuildServiceProvider();
        ICommandHandler<StartSagaCommand<TestSagaInput>, TestSagaDefinition>? handler =
            provider.GetService<ICommandHandler<StartSagaCommand<TestSagaInput>, TestSagaDefinition>>();
        Assert.NotNull(handler);
    }
}