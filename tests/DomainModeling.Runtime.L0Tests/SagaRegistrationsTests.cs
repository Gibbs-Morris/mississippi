using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Brooks.Abstractions.Attributes;
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
        string legacyEventName = EventStorageNameHelper.GetStorageName(typeof(SagaInputProvided<>));
        Assert.NotNull(firstEventName);
        Assert.NotNull(secondEventName);
        Assert.NotEqual(firstEventName, secondEventName);
        Assert.Equal(typeof(SagaInputProvided<TestInput>), registry.ResolveType(firstEventName));
        Assert.Equal(typeof(SagaInputProvided<SecondTestInput>), registry.ResolveType(secondEventName));
        Assert.Equal(typeof(SagaInputProvided<TestInput>), registry.ResolveType(legacyEventName));
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