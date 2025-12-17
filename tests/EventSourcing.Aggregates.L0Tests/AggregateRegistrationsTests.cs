using System;
using System.Collections.Generic;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.EventSourcing.Abstractions.Attributes;
using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Mississippi.EventSourcing.Aggregates.Tests;

/// <summary>
///     Tests for <see cref="AggregateRegistrations" />.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Aggregates")]
[AllureSubSuite("Registrations")]
public class AggregateRegistrationsTests
{
    /// <summary>
    ///     Test command record.
    /// </summary>
    /// <param name="Value">The command value.</param>
    private sealed record TestCommand(string Value);

    /// <summary>
    ///     Test command handler.
    /// </summary>
    private sealed class TestCommandHandler : ICommandHandler<TestCommand, TestState>
    {
        /// <inheritdoc />
        public OperationResult<IReadOnlyList<object>> Handle(
            TestCommand command,
            TestState? state
        ) =>
            OperationResult.Ok<IReadOnlyList<object>>(Array.Empty<object>());

        /// <inheritdoc />
        public bool TryHandle(
            object command,
            TestState? state,
            out OperationResult<IReadOnlyList<object>> result
        )
        {
            if (command is TestCommand typedCommand)
            {
                result = Handle(typedCommand, state);
                return true;
            }

            result = default!;
            return false;
        }
    }

    /// <summary>
    ///     Test event class.
    /// </summary>
    [EventName("TEST", "APP", "TESTEVENT")]
    private sealed class TestEvent;

    /// <summary>
    ///     Test state record.
    /// </summary>
    /// <param name="Count">The state count.</param>
    private sealed record TestState(int Count);

    /// <summary>
    ///     AddAggregateSupport should register IEventTypeRegistry.
    /// </summary>
    [Fact]
    public void AddAggregateSupportRegistersEventTypeRegistry()
    {
        ServiceCollection services = new();
        services.AddAggregateSupport();
        using ServiceProvider provider = services.BuildServiceProvider();
        IEventTypeRegistry? registry = provider.GetService<IEventTypeRegistry>();
        Assert.NotNull(registry);
        Assert.IsType<EventTypeRegistry>(registry);
    }

    /// <summary>
    ///     AddAggregateSupport should return the service collection for chaining.
    /// </summary>
    [Fact]
    public void AddAggregateSupportReturnsServiceCollection()
    {
        ServiceCollection services = new();
        IServiceCollection result = services.AddAggregateSupport();
        Assert.Same(services, result);
    }

    /// <summary>
    ///     AddAggregateSupport should throw when services is null.
    /// </summary>
    [Fact]
    public void AddAggregateSupportThrowsWhenServicesIsNull()
    {
        IServiceCollection? services = null;
        Assert.Throws<ArgumentNullException>(() => services!.AddAggregateSupport());
    }

    /// <summary>
    ///     AddCommandHandler with class should register the handler.
    /// </summary>
    [Fact]
    public void AddCommandHandlerWithClassRegistersHandler()
    {
        ServiceCollection services = new();
        services.AddCommandHandler<TestCommand, TestState, TestCommandHandler>();
        using ServiceProvider provider = services.BuildServiceProvider();
        ICommandHandler<TestCommand, TestState>? handler =
            provider.GetService<ICommandHandler<TestCommand, TestState>>();
        Assert.NotNull(handler);
        Assert.IsType<TestCommandHandler>(handler);
    }

    /// <summary>
    ///     AddCommandHandler with class should throw when services is null.
    /// </summary>
    [Fact]
    public void AddCommandHandlerWithClassThrowsWhenServicesIsNull()
    {
        IServiceCollection? services = null;
        Assert.Throws<ArgumentNullException>(() =>
            services!.AddCommandHandler<TestCommand, TestState, TestCommandHandler>());
    }

    /// <summary>
    ///     AddCommandHandler with delegate should register the handler.
    /// </summary>
    [Fact]
    public void AddCommandHandlerWithDelegateRegistersHandler()
    {
        ServiceCollection services = new();
        bool delegateCalled = false;
        services.AddCommandHandler<TestCommand, TestState>((
            _,
            _
        ) =>
        {
            delegateCalled = true;
            return OperationResult.Ok<IReadOnlyList<object>>(Array.Empty<object>());
        });
        using ServiceProvider provider = services.BuildServiceProvider();
        ICommandHandler<TestCommand, TestState>? handler =
            provider.GetService<ICommandHandler<TestCommand, TestState>>();
        Assert.NotNull(handler);
        handler.Handle(new("test"), null);
        Assert.True(delegateCalled);
    }

    /// <summary>
    ///     AddCommandHandler with delegate should throw when handler is null.
    /// </summary>
    [Fact]
    public void AddCommandHandlerWithDelegateThrowsWhenHandlerIsNull()
    {
        ServiceCollection services = new();
        Assert.Throws<ArgumentNullException>(() => services.AddCommandHandler<TestCommand, TestState>(null!));
    }

    /// <summary>
    ///     AddCommandHandler with delegate should throw when services is null.
    /// </summary>
    [Fact]
    public void AddCommandHandlerWithDelegateThrowsWhenServicesIsNull()
    {
        IServiceCollection? services = null;
        Assert.Throws<ArgumentNullException>(() => services!.AddCommandHandler<TestCommand, TestState>((
            _,
            _
        ) => OperationResult.Ok<IReadOnlyList<object>>(Array.Empty<object>())));
    }

    /// <summary>
    ///     AddEventType should register aggregate support.
    /// </summary>
    [Fact]
    public void AddEventTypeRegistersAggregateSupport()
    {
        ServiceCollection services = new();
        services.AddEventType<TestEvent>();
        using ServiceProvider provider = services.BuildServiceProvider();
        IEventTypeRegistry? registry = provider.GetService<IEventTypeRegistry>();
        Assert.NotNull(registry);
    }

    /// <summary>
    ///     AddEventType should register the event with the registry.
    /// </summary>
    [Fact]
    public void AddEventTypeRegistersEventWithRegistry()
    {
        ServiceCollection services = new();
        services.AddEventType<TestEvent>();
        using ServiceProvider provider = services.BuildServiceProvider();
        IEventTypeRegistry registry = provider.GetRequiredService<IEventTypeRegistry>();
        string eventName = EventNameHelper.GetEventName<TestEvent>();
        Assert.Equal(typeof(TestEvent), registry.ResolveType(eventName));
    }

    /// <summary>
    ///     AddEventType should return the service collection for chaining.
    /// </summary>
    [Fact]
    public void AddEventTypeReturnsServiceCollection()
    {
        ServiceCollection services = new();
        IServiceCollection result = services.AddEventType<TestEvent>();
        Assert.Same(services, result);
    }

    /// <summary>
    ///     AddEventType should throw when services is null.
    /// </summary>
    [Fact]
    public void AddEventTypeThrowsWhenServicesIsNull()
    {
        IServiceCollection? services = null;
        Assert.Throws<ArgumentNullException>(() => services!.AddEventType<TestEvent>());
    }
}