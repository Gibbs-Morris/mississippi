using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Abstractions.Builders;
using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;


namespace Mississippi.EventSourcing.Aggregates.L0Tests;

/// <summary>
///     Tests for <see cref="AggregateRegistrations" />.
/// </summary>
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
    [EventStorageName("TEST", "APP", "TESTEVENT")]
    private sealed class TestEvent;

    /// <summary>
    ///     Test event effect implementation.
    /// </summary>
    private sealed class TestEventEffect : EventEffectBase<TestEvent, TestState>
    {
        /// <inheritdoc />
        public override IAsyncEnumerable<object> HandleAsync(
            TestEvent eventData,
            TestState currentState,
            string brookKey,
            long eventPosition,
            CancellationToken cancellationToken
        ) =>
            AsyncEnumerable.Empty<object>();
    }

    /// <summary>
    ///     Test fire-and-forget event effect implementation.
    /// </summary>
    private sealed class TestFireAndForgetEffect : IFireAndForgetEventEffect<TestEvent, TestState>
    {
        /// <inheritdoc />
        public Task HandleAsync(
            TestEvent eventData,
            TestState aggregateState,
            string brookKey,
            long eventPosition,
            CancellationToken cancellationToken
        ) =>
            Task.CompletedTask;
    }

    private sealed class TestMississippiSiloBuilder : IMississippiSiloBuilder
    {
        private IServiceCollection Services { get; }

        public TestMississippiSiloBuilder(
            IServiceCollection services
        )
        {
            ArgumentNullException.ThrowIfNull(services);
            Services = services;
        }

        public IMississippiSiloBuilder ConfigureOptions<TOptions>(
            Action<TOptions> configure
        )
            where TOptions : class
        {
            Services.Configure(configure);
            return this;
        }

        public IMississippiSiloBuilder ConfigureServices(
            Action<IServiceCollection> configure
        )
        {
            configure(Services);
            return this;
        }
    }

    private static (ServiceCollection Services, TestMississippiSiloBuilder Builder) CreateBuilder()
    {
        ServiceCollection services = [];
        return (services, new TestMississippiSiloBuilder(services));
    }

    /// <summary>
    ///     Test snapshot class.
    /// </summary>
    [SnapshotStorageName("TEST", "APP", "TESTSNAPSHOT")]
    private sealed class TestSnapshot;

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
        var (services, builder) = CreateBuilder();
        builder.AddAggregateSupport();
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
        TestMississippiSiloBuilder builder = new(new ServiceCollection());
        IMississippiSiloBuilder result = builder.AddAggregateSupport();
        Assert.Same(builder, result);
    }

    /// <summary>
    ///     AddAggregateSupport should throw when services is null.
    /// </summary>
    [Fact]
    public void AddAggregateSupportThrowsWhenServicesIsNull()
    {
        IMississippiSiloBuilder? builder = null;
        Assert.Throws<ArgumentNullException>(() => builder!.AddAggregateSupport());
    }

    /// <summary>
    ///     AddCommandHandler with class should register the handler.
    /// </summary>
    [Fact]
    public void AddCommandHandlerWithClassRegistersHandler()
    {
        var (services, builder) = CreateBuilder();
        builder.AddCommandHandler<TestCommand, TestState, TestCommandHandler>();
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
        IMississippiSiloBuilder? builder = null;
        Assert.Throws<ArgumentNullException>(() =>
            builder!.AddCommandHandler<TestCommand, TestState, TestCommandHandler>());
    }

    /// <summary>
    ///     AddCommandHandler with delegate should register the handler.
    /// </summary>
    [Fact]
    public void AddCommandHandlerWithDelegateRegistersHandler()
    {
        var (services, builder) = CreateBuilder();
        bool delegateCalled = false;
        builder.AddCommandHandler<TestCommand, TestState>((
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
        TestMississippiSiloBuilder builder = new(new ServiceCollection());
        Assert.Throws<ArgumentNullException>(() => builder.AddCommandHandler<TestCommand, TestState>(null!));
    }

    /// <summary>
    ///     AddCommandHandler with delegate should throw when services is null.
    /// </summary>
    [Fact]
    public void AddCommandHandlerWithDelegateThrowsWhenServicesIsNull()
    {
        IMississippiSiloBuilder? builder = null;
        Assert.Throws<ArgumentNullException>(() => builder!.AddCommandHandler<TestCommand, TestState>((
            _,
            _
        ) => OperationResult.Ok<IReadOnlyList<object>>(Array.Empty<object>())));
    }

    /// <summary>
    ///     AddEventEffect should register the effect in DI.
    /// </summary>
    [Fact]
    public void AddEventEffectRegistersEffectInDI()
    {
        // Arrange
        var (services, builder) = CreateBuilder();

        // Act
        builder.AddEventEffect<TestEventEffect, TestState>();
        using ServiceProvider provider = services.BuildServiceProvider();
        IEnumerable<IEventEffect<TestState>> effects = provider.GetServices<IEventEffect<TestState>>();

        // Assert
        Assert.Single(effects);
        Assert.IsType<TestEventEffect>(effects.First());
    }

    /// <summary>
    ///     AddEventEffect should register root event effect.
    /// </summary>
    [Fact]
    public void AddEventEffectRegistersRootEventEffect()
    {
        // Arrange
        var (services, builder) = CreateBuilder();

        // Act
        builder.AddEventEffect<TestEventEffect, TestState>();
        using ServiceProvider provider = services.BuildServiceProvider();
        IRootEventEffect<TestState>? rootEffect = provider.GetService<IRootEventEffect<TestState>>();

        // Assert
        Assert.NotNull(rootEffect);
        Assert.IsType<RootEventEffect<TestState>>(rootEffect);
    }

    /// <summary>
    ///     AddEventEffect should throw when services is null.
    /// </summary>
    [Fact]
    public void AddEventEffectThrowsWhenServicesIsNull()
    {
        // Arrange
        IMississippiSiloBuilder? builder = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder!.AddEventEffect<TestEventEffect, TestState>());
    }

    /// <summary>
    ///     AddEventType should register aggregate support.
    /// </summary>
    [Fact]
    public void AddEventTypeRegistersAggregateSupport()
    {
        var (services, builder) = CreateBuilder();
        builder.AddEventType<TestEvent>();
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
        var (services, builder) = CreateBuilder();
        builder.AddEventType<TestEvent>();
        using ServiceProvider provider = services.BuildServiceProvider();
        IEventTypeRegistry registry = provider.GetRequiredService<IEventTypeRegistry>();
        string eventName = EventStorageNameHelper.GetStorageName<TestEvent>();
        Assert.Equal(typeof(TestEvent), registry.ResolveType(eventName));
    }

    /// <summary>
    ///     AddEventType should return the service collection for chaining.
    /// </summary>
    [Fact]
    public void AddEventTypeReturnsServiceCollection()
    {
        TestMississippiSiloBuilder builder = new(new ServiceCollection());
        IMississippiSiloBuilder result = builder.AddEventType<TestEvent>();
        Assert.Same(builder, result);
    }

    /// <summary>
    ///     AddEventType should throw when services is null.
    /// </summary>
    [Fact]
    public void AddEventTypeThrowsWhenServicesIsNull()
    {
        IMississippiSiloBuilder? builder = null;
        Assert.Throws<ArgumentNullException>(() => builder!.AddEventType<TestEvent>());
    }

    /// <summary>
    ///     AddFireAndForgetEventEffect should register the effect in DI.
    /// </summary>
    [Fact]
    public void AddFireAndForgetEventEffectRegistersEffectInDI()
    {
        // Arrange
        var (services, builder) = CreateBuilder();

        // Act
        builder.AddFireAndForgetEventEffect<TestFireAndForgetEffect, TestEvent, TestState>();
        using ServiceProvider provider = services.BuildServiceProvider();
        IFireAndForgetEventEffect<TestEvent, TestState>? effect =
            provider.GetService<IFireAndForgetEventEffect<TestEvent, TestState>>();

        // Assert
        Assert.NotNull(effect);
        Assert.IsType<TestFireAndForgetEffect>(effect);
    }

    /// <summary>
    ///     AddFireAndForgetEventEffect should register the effect registration for grain discovery.
    /// </summary>
    [Fact]
    public void AddFireAndForgetEventEffectRegistersEffectRegistration()
    {
        // Arrange
        var (services, builder) = CreateBuilder();

        // Act
        builder.AddFireAndForgetEventEffect<TestFireAndForgetEffect, TestEvent, TestState>();
        using ServiceProvider provider = services.BuildServiceProvider();
        IEnumerable<IFireAndForgetEffectRegistration<TestState>> registrations =
            provider.GetServices<IFireAndForgetEffectRegistration<TestState>>();

        // Assert
        IFireAndForgetEffectRegistration<TestState>? registration = registrations.SingleOrDefault();
        Assert.NotNull(registration);
        Assert.Equal(typeof(TestEvent), registration.EventType);
        Assert.Equal(typeof(TestFireAndForgetEffect).FullName, registration.EffectTypeName);
    }

    /// <summary>
    ///     AddFireAndForgetEventEffect should return the service collection for chaining.
    /// </summary>
    [Fact]
    public void AddFireAndForgetEventEffectReturnsServiceCollection()
    {
        // Arrange
        TestMississippiSiloBuilder builder = new(new ServiceCollection());

        // Act
        IMississippiSiloBuilder result =
            builder.AddFireAndForgetEventEffect<TestFireAndForgetEffect, TestEvent, TestState>();

        // Assert
        Assert.Same(builder, result);
    }

    /// <summary>
    ///     AddFireAndForgetEventEffect should throw when services is null.
    /// </summary>
    [Fact]
    public void AddFireAndForgetEventEffectThrowsWhenServicesIsNull()
    {
        // Arrange
        IMississippiSiloBuilder? builder = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            builder!.AddFireAndForgetEventEffect<TestFireAndForgetEffect, TestEvent, TestState>());
    }

    /// <summary>
    ///     AddRootCommandHandler should register the handler.
    /// </summary>
    [Fact]
    public void AddRootCommandHandlerRegistersHandler()
    {
        var (services, builder) = CreateBuilder();
        builder.AddAggregateSupport();
        builder.AddRootCommandHandler<TestState>();
        using ServiceProvider provider = services.BuildServiceProvider();
        IRootCommandHandler<TestState>? handler = provider.GetService<IRootCommandHandler<TestState>>();
        Assert.NotNull(handler);
    }

    /// <summary>
    ///     AddRootEventEffect should register root event effect.
    /// </summary>
    [Fact]
    public void AddRootEventEffectRegistersRootEventEffect()
    {
        // Arrange
        var (services, builder) = CreateBuilder();

        // Act
        builder.AddRootEventEffect<TestState>();
        using ServiceProvider provider = services.BuildServiceProvider();
        IRootEventEffect<TestState>? rootEffect = provider.GetService<IRootEventEffect<TestState>>();

        // Assert
        Assert.NotNull(rootEffect);
    }

    /// <summary>
    ///     AddRootEventEffect should throw when services is null.
    /// </summary>
    [Fact]
    public void AddRootEventEffectThrowsWhenServicesIsNull()
    {
        // Arrange
        IMississippiSiloBuilder? builder = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder!.AddRootEventEffect<TestState>());
    }

    /// <summary>
    ///     AddSnapshotType should register aggregate support.
    /// </summary>
    [Fact]
    public void AddSnapshotTypeRegistersAggregateSupport()
    {
        var (services, builder) = CreateBuilder();
        builder.AddSnapshotType<TestSnapshot>();
        using ServiceProvider provider = services.BuildServiceProvider();
        ISnapshotTypeRegistry? registry = provider.GetService<ISnapshotTypeRegistry>();
        Assert.NotNull(registry);
    }

    /// <summary>
    ///     AddSnapshotType should register the snapshot with the registry.
    /// </summary>
    [Fact]
    public void AddSnapshotTypeRegistersSnapshotWithRegistry()
    {
        var (services, builder) = CreateBuilder();
        builder.AddSnapshotType<TestSnapshot>();
        using ServiceProvider provider = services.BuildServiceProvider();
        ISnapshotTypeRegistry registry = provider.GetRequiredService<ISnapshotTypeRegistry>();
        string snapshotName = SnapshotStorageNameHelper.GetStorageName<TestSnapshot>();
        Assert.Equal(typeof(TestSnapshot), registry.ResolveType(snapshotName));
    }

    /// <summary>
    ///     AddSnapshotType should return the service collection for chaining.
    /// </summary>
    [Fact]
    public void AddSnapshotTypeReturnsServiceCollection()
    {
        TestMississippiSiloBuilder builder = new(new ServiceCollection());
        IMississippiSiloBuilder result = builder.AddSnapshotType<TestSnapshot>();
        Assert.Same(builder, result);
    }

    /// <summary>
    ///     AddSnapshotType should throw when services is null.
    /// </summary>
    [Fact]
    public void AddSnapshotTypeThrowsWhenServicesIsNull()
    {
        IMississippiSiloBuilder? builder = null;
        Assert.Throws<ArgumentNullException>(() => builder!.AddSnapshotType<TestSnapshot>());
    }

    /// <summary>
    ///     ScanAssemblyForEventTypes should register aggregate support.
    /// </summary>
    [Fact]
    public void ScanAssemblyForEventTypesRegistersAggregateSupport()
    {
        var (services, builder) = CreateBuilder();
        builder.ScanAssemblyForEventTypes(typeof(TestEvent).Assembly);
        using ServiceProvider provider = services.BuildServiceProvider();
        IEventTypeRegistry? registry = provider.GetService<IEventTypeRegistry>();
        Assert.NotNull(registry);
    }

    /// <summary>
    ///     ScanAssemblyForEventTypes should return the service collection for chaining.
    /// </summary>
    [Fact]
    public void ScanAssemblyForEventTypesReturnsServiceCollection()
    {
        TestMississippiSiloBuilder builder = new(new ServiceCollection());
        IMississippiSiloBuilder result = builder.ScanAssemblyForEventTypes(typeof(TestEvent).Assembly);
        Assert.Same(builder, result);
    }

    /// <summary>
    ///     ScanAssemblyForEventTypes should throw when assembly is null.
    /// </summary>
    [Fact]
    public void ScanAssemblyForEventTypesThrowsWhenAssemblyIsNull()
    {
        TestMississippiSiloBuilder builder = new(new ServiceCollection());
        Assert.Throws<ArgumentNullException>(() => builder.ScanAssemblyForEventTypes(null!));
    }

    /// <summary>
    ///     ScanAssemblyForEventTypes should throw when services is null.
    /// </summary>
    [Fact]
    public void ScanAssemblyForEventTypesThrowsWhenServicesIsNull()
    {
        IMississippiSiloBuilder? builder = null;
        Assert.Throws<ArgumentNullException>(() => builder!.ScanAssemblyForEventTypes(typeof(TestEvent).Assembly));
    }

    /// <summary>
    ///     ScanAssemblyForEventTypes with marker type should register aggregate support.
    /// </summary>
    [Fact]
    public void ScanAssemblyForEventTypesWithMarkerRegistersAggregateSupport()
    {
        var (services, builder) = CreateBuilder();
        builder.ScanAssemblyForEventTypes<TestEvent>();
        using ServiceProvider provider = services.BuildServiceProvider();
        IEventTypeRegistry? registry = provider.GetService<IEventTypeRegistry>();
        Assert.NotNull(registry);
    }

    /// <summary>
    ///     ScanAssemblyForSnapshotTypes should register aggregate support.
    /// </summary>
    [Fact]
    public void ScanAssemblyForSnapshotTypesRegistersAggregateSupport()
    {
        var (services, builder) = CreateBuilder();
        builder.ScanAssemblyForSnapshotTypes(typeof(TestSnapshot).Assembly);
        using ServiceProvider provider = services.BuildServiceProvider();
        ISnapshotTypeRegistry? registry = provider.GetService<ISnapshotTypeRegistry>();
        Assert.NotNull(registry);
    }

    /// <summary>
    ///     ScanAssemblyForSnapshotTypes should return the service collection for chaining.
    /// </summary>
    [Fact]
    public void ScanAssemblyForSnapshotTypesReturnsServiceCollection()
    {
        TestMississippiSiloBuilder builder = new(new ServiceCollection());
        IMississippiSiloBuilder result = builder.ScanAssemblyForSnapshotTypes(typeof(TestSnapshot).Assembly);
        Assert.Same(builder, result);
    }

    /// <summary>
    ///     ScanAssemblyForSnapshotTypes should throw when assembly is null.
    /// </summary>
    [Fact]
    public void ScanAssemblyForSnapshotTypesThrowsWhenAssemblyIsNull()
    {
        TestMississippiSiloBuilder builder = new(new ServiceCollection());
        Assert.Throws<ArgumentNullException>(() => builder.ScanAssemblyForSnapshotTypes(null!));
    }

    /// <summary>
    ///     ScanAssemblyForSnapshotTypes should throw when services is null.
    /// </summary>
    [Fact]
    public void ScanAssemblyForSnapshotTypesThrowsWhenServicesIsNull()
    {
        IMississippiSiloBuilder? builder = null;
        Assert.Throws<ArgumentNullException>(() =>
            builder!.ScanAssemblyForSnapshotTypes(typeof(TestSnapshot).Assembly));
    }

    /// <summary>
    ///     ScanAssemblyForSnapshotTypes with marker type should register aggregate support.
    /// </summary>
    [Fact]
    public void ScanAssemblyForSnapshotTypesWithMarkerRegistersAggregateSupport()
    {
        var (services, builder) = CreateBuilder();
        builder.ScanAssemblyForSnapshotTypes<TestSnapshot>();
        using ServiceProvider provider = services.BuildServiceProvider();
        ISnapshotTypeRegistry? registry = provider.GetService<ISnapshotTypeRegistry>();
        Assert.NotNull(registry);
    }
}