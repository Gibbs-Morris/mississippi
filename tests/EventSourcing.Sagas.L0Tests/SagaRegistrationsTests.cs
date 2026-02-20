using System;
using Microsoft.Extensions.DependencyInjection;

using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions;


namespace Mississippi.EventSourcing.Sagas.L0Tests;

/// <summary>
///     Tests for <see cref="SagaRegistrations" />.
/// </summary>
public sealed class SagaRegistrationsTests
{
    private sealed record TestInput(string Value);

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
    ///     Verifies saga resume requested event type is registered.
    /// </summary>
    [Fact]
    public void AddSagaOrchestrationRegistersSagaResumeRequestedEventType()
    {
        ServiceCollection services = new();
        services.AddSagaOrchestration<TestSagaState, TestInput>();
        using ServiceProvider provider = services.BuildServiceProvider();
        IEventTypeRegistry registry = provider.GetRequiredService<IEventTypeRegistry>();
        string? eventName = registry.ResolveName(typeof(SagaResumeRequested));
        Assert.NotNull(eventName);
    }

    /// <summary>
    ///     Verifies continue-saga command handler is registered.
    /// </summary>
    [Fact]
    public void AddSagaOrchestrationRegistersContinueSagaCommandHandler()
    {
        ServiceCollection services = new();
        services.AddSagaOrchestration<TestSagaState, TestInput>();
        using ServiceProvider provider = services.BuildServiceProvider();
        ICommandHandler<ContinueSagaCommand, TestSagaState>? handler = provider
            .GetService<ICommandHandler<ContinueSagaCommand, TestSagaState>>();
        Assert.NotNull(handler);
        Assert.IsType<ContinueSagaCommandHandler<TestSagaState>>(handler);
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