using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.DomainModeling.Abstractions;


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
}