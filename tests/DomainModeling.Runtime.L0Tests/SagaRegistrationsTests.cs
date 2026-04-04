using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Mississippi.Brooks.Abstractions.Factory;
using Mississippi.DomainModeling.Abstractions;
using Mississippi.Tributary.Abstractions;


namespace Mississippi.DomainModeling.Runtime.L0Tests;

/// <summary>
///     Tests for <see cref="SagaRegistrations" />.
/// </summary>
public sealed class SagaRegistrationsTests
{
    private sealed record SecondTestInput(string Value);

    private sealed record SecondTestSagaState : ISagaState
    {
        public string? CorrelationId { get; init; }

        public int LastCompletedStepIndex { get; } = -1;

        public SagaPhase Phase { get; init; }

        public Guid SagaId { get; init; }

        public DateTimeOffset? StartedAt { get; init; }

        public string? StepHash { get; init; }
    }

    private sealed record TestInput(string Value);

    /// <summary>
    ///     Verifies an explicit saga recovery provider registration overrides the default.
    /// </summary>
    [Fact]
    public void AddSagaOrchestrationAllowsExplicitSagaRecoveryInfoOverride()
    {
        ServiceCollection services = new();
        services.AddSagaOrchestration<TestSagaState, TestInput>();
        services.AddSagaRecoveryInfo<TestSagaState>(new(SagaRecoveryMode.ManualOnly, "critical-payments"));
        using ServiceProvider provider = services.BuildServiceProvider();
        ISagaRecoveryInfoProvider<TestSagaState> recoveryProvider =
            provider.GetRequiredService<ISagaRecoveryInfoProvider<TestSagaState>>();
        Assert.Equal(SagaRecoveryMode.ManualOnly, recoveryProvider.Recovery.Mode);
        Assert.Equal("critical-payments", recoveryProvider.Recovery.Profile);
    }

    /// <summary>
    ///     Verifies a default saga recovery provider is registered when none is supplied.
    /// </summary>
    [Fact]
    public void AddSagaOrchestrationRegistersDefaultSagaRecoveryInfoProvider()
    {
        ServiceCollection services = new();
        services.AddSagaOrchestration<TestSagaState, TestInput>();
        using ServiceProvider provider = services.BuildServiceProvider();
        ISagaRecoveryInfoProvider<TestSagaState> recoveryProvider =
            provider.GetRequiredService<ISagaRecoveryInfoProvider<TestSagaState>>();
        Assert.Equal(SagaRecoveryMode.Automatic, recoveryProvider.Recovery.Mode);
        Assert.Null(recoveryProvider.Recovery.Profile);
    }

    /// <summary>
    ///     Verifies multiple saga orchestrations can register distinct saga input event types.
    /// </summary>
    [Fact]
    public void AddSagaOrchestrationRegistersDistinctNamesForMultipleSagaInputTypes()
    {
        ServiceCollection services = new();
        services.AddSagaOrchestration<TestSagaState, TestInput>();
        services.AddSagaOrchestration<SecondTestSagaState, SecondTestInput>();
        using ServiceProvider provider = services.BuildServiceProvider();
        IEventTypeRegistry registry = provider.GetRequiredService<IEventTypeRegistry>();
        string? firstEventName = registry.ResolveName(typeof(SagaInputProvided<TestInput>));
        string? secondEventName = registry.ResolveName(typeof(SagaInputProvided<SecondTestInput>));
        Assert.NotNull(firstEventName);
        Assert.NotNull(secondEventName);
        Assert.NotEqual(firstEventName, secondEventName);
        Assert.Equal(typeof(SagaInputProvided<TestInput>), registry.ResolveType(firstEventName));
        Assert.Equal(typeof(SagaInputProvided<SecondTestInput>), registry.ResolveType(secondEventName));
    }

    /// <summary>
    ///     Verifies the recovery coordinator service is registered.
    /// </summary>
    [Fact]
    public void AddSagaOrchestrationRegistersRecoveryCoordinator()
    {
        ServiceCollection services = new();
        services.AddSagaOrchestration<TestSagaState, TestInput>();
        Assert.Contains(
            services,
            descriptor => (descriptor.ServiceType == typeof(SagaRecoveryCoordinator<TestSagaState>)) &&
                          (descriptor.ImplementationType == typeof(SagaRecoveryCoordinator<TestSagaState>)) &&
                          (descriptor.Lifetime == ServiceLifetime.Transient));
    }

    /// <summary>
    ///     Verifies the recovery planner service is registered.
    /// </summary>
    [Fact]
    public void AddSagaOrchestrationRegistersRecoveryPlanner()
    {
        ServiceCollection services = new();
        services.AddSagaOrchestration<TestSagaState, TestInput>();
        Assert.Contains(
            services,
            descriptor => (descriptor.ServiceType == typeof(SagaRecoveryPlanner<TestSagaState>)) &&
                          (descriptor.ImplementationType == typeof(SagaRecoveryPlanner<TestSagaState>)) &&
                          (descriptor.Lifetime == ServiceLifetime.Transient));
    }

    /// <summary>
    ///     Verifies the resume command handler is registered.
    /// </summary>
    [Fact]
    public void AddSagaOrchestrationRegistersResumeSagaCommandHandler()
    {
        ServiceCollection services = new();
        services.AddSagaOrchestration<TestSagaState, TestInput>();
        Assert.Contains(
            services,
            descriptor => (descriptor.ServiceType == typeof(ICommandHandler<ResumeSagaCommand, TestSagaState>)) &&
                          (descriptor.ImplementationType == typeof(ResumeSagaCommandHandler<TestSagaState>)) &&
                          (descriptor.Lifetime == ServiceLifetime.Transient));
    }

    /// <summary>
    ///     Verifies saga input event type is registered.
    /// </summary>
    [Fact]
    public void AddSagaOrchestrationRegistersSagaInputEventType()
    {
        ServiceCollection services = new();
        services.AddSagaOrchestration<TestSagaState, TestInput>();
        using ServiceProvider provider = services.BuildServiceProvider();
        IEventTypeRegistry registry = provider.GetRequiredService<IEventTypeRegistry>();
        string? eventName = registry.ResolveName(typeof(SagaInputProvided<TestInput>));
        Assert.NotNull(eventName);
    }

    /// <summary>
    ///     Verifies saga recovery options are registered.
    /// </summary>
    [Fact]
    public void AddSagaOrchestrationRegistersSagaRecoveryOptions()
    {
        ServiceCollection services = new();
        services.AddSagaOrchestration<TestSagaState, TestInput>();
        using ServiceProvider provider = services.BuildServiceProvider();
        IOptions<SagaRecoveryOptions> options = provider.GetRequiredService<IOptions<SagaRecoveryOptions>>();
        Assert.True(options.Value.Enabled);
    }

    /// <summary>
    ///     Verifies the public saga recovery service is registered.
    /// </summary>
    [Fact]
    public void AddSagaOrchestrationRegistersSagaRecoveryService()
    {
        ServiceCollection services = new();
        services.AddSagaOrchestration<TestSagaState, TestInput>();
        Assert.Contains(
            services,
            descriptor => (descriptor.ServiceType == typeof(ISagaRecoveryService<TestSagaState>)) &&
                          (descriptor.ImplementationType == typeof(SagaRecoveryService<TestSagaState>)) &&
                          (descriptor.Lifetime == ServiceLifetime.Transient));
    }

    /// <summary>
    ///     Verifies the saga reminder handler is registered.
    /// </summary>
    [Fact]
    public void AddSagaOrchestrationRegistersSagaReminderHandler()
    {
        ServiceCollection services = new();
        services.AddSagaOrchestration<TestSagaState, TestInput>();
        Assert.Contains(
            services,
            descriptor => (descriptor.ServiceType == typeof(IAggregateReminderHandler<TestSagaState>)) &&
                          (descriptor.ImplementationType == typeof(SagaReminderHandler<TestSagaState>)) &&
                          (descriptor.Lifetime == ServiceLifetime.Transient));
    }

    /// <summary>
    ///     Verifies the saga reminder reconciler is registered.
    /// </summary>
    [Fact]
    public void AddSagaOrchestrationRegistersSagaReminderReconciler()
    {
        ServiceCollection services = new();
        services.AddSagaOrchestration<TestSagaState, TestInput>();
        Assert.Contains(
            services,
            descriptor => (descriptor.ServiceType == typeof(IAggregateReminderReconciler<TestSagaState>)) &&
                          (descriptor.ImplementationType == typeof(SagaReminderReconciler<TestSagaState>)) &&
                          (descriptor.Lifetime == ServiceLifetime.Transient));
    }

    /// <summary>
    ///     Verifies blocked-resume event type is registered.
    /// </summary>
    [Fact]
    public void AddSagaOrchestrationRegistersSagaResumeBlockedEventType()
    {
        ServiceCollection services = new();
        services.AddSagaOrchestration<TestSagaState, TestInput>();
        using ServiceProvider provider = services.BuildServiceProvider();
        IEventTypeRegistry registry = provider.GetRequiredService<IEventTypeRegistry>();
        string? eventName = registry.ResolveName(typeof(SagaResumeBlocked));
        Assert.NotNull(eventName);
        Assert.Equal(typeof(SagaResumeBlocked), registry.ResolveType(eventName));
    }

    /// <summary>
    ///     Verifies service collection chaining.
    /// </summary>
    [Fact]
    public void AddSagaOrchestrationReturnsServiceCollection()
    {
        ServiceCollection services = new();
        IServiceCollection result = services.AddSagaOrchestration<TestSagaState, TestInput>();
        Assert.Same(services, result);
    }

    /// <summary>
    ///     Verifies null services throws.
    /// </summary>
    [Fact]
    public void AddSagaOrchestrationThrowsWhenServicesNull()
    {
        IServiceCollection? services = null;
        Assert.Throws<ArgumentNullException>(() => services!.AddSagaOrchestration<TestSagaState, TestInput>());
    }

    /// <summary>
    ///     Verifies gateway-safe saga recovery registrations include the Brooks and snapshot factory services they depend on.
    /// </summary>
    [Fact]
    public void AddSagaRecoveryServicesRegistersBrookAndSnapshotFactories()
    {
        ServiceCollection services = new();
        services.AddSagaRecoveryServices<TestSagaState>();
        Assert.Contains(
            services,
            descriptor => (descriptor.ServiceType == typeof(IBrookGrainFactory)) &&
                          (descriptor.Lifetime == ServiceLifetime.Singleton));
        Assert.Contains(
            services,
            descriptor => (descriptor.ServiceType == typeof(ISnapshotGrainFactory)) &&
                          (descriptor.Lifetime == ServiceLifetime.Singleton));
    }
}