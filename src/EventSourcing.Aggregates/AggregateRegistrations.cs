using System;
using System.Collections.Generic;
using System.Reflection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.Common.Abstractions.Builders;
using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;


namespace Mississippi.EventSourcing.Aggregates;

/// <summary>
///     Provides extension methods for registering aggregate components in the dependency injection container.
/// </summary>
public static class AggregateRegistrations
{
    /// <summary>
    ///     Adds aggregate infrastructure services to the service collection.
    /// </summary>
    /// <param name="builder">The Mississippi silo builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IMississippiSiloBuilder AddAggregateSupport(
        this IMississippiSiloBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ConfigureServices(AddAggregateSupportServices);
        return builder;
    }

    /// <summary>
    ///     Adds aggregate infrastructure services to the service collection.
    /// </summary>
    /// <param name="builder">The Mississippi server builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IMississippiServerBuilder AddAggregateSupport(
        this IMississippiServerBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ConfigureServices(AddAggregateSupportServices);
        return builder;
    }

    /// <summary>
    ///     Registers a command handler for processing commands against aggregate state.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <typeparam name="TSnapshot">The aggregate state type.</typeparam>
    /// <typeparam name="THandler">The command handler implementation type.</typeparam>
    /// <param name="builder">The Mississippi silo builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IMississippiSiloBuilder AddCommandHandler<TCommand, TSnapshot, THandler>(
        this IMississippiSiloBuilder builder
    )
        where THandler : class, ICommandHandler<TCommand, TSnapshot>
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ConfigureServices(services => AddCommandHandlerServices<TCommand, TSnapshot, THandler>(services));
        return builder;
    }

    /// <summary>
    ///     Registers a command handler using a delegate for processing commands against aggregate state.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <typeparam name="TSnapshot">The aggregate state type.</typeparam>
    /// <param name="builder">The Mississippi silo builder.</param>
    /// <param name="handler">The delegate that handles the command.</param>
    /// <returns>The builder for chaining.</returns>
    public static IMississippiSiloBuilder AddCommandHandler<TCommand, TSnapshot>(
        this IMississippiSiloBuilder builder,
        Func<TCommand, TSnapshot?, OperationResult<IReadOnlyList<object>>> handler
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(handler);
        builder.ConfigureServices(services => AddCommandHandlerServices(services, handler));
        return builder;
    }

    /// <summary>
    ///     Registers an event effect for processing events on a specific aggregate type.
    /// </summary>
    /// <typeparam name="TEffect">The effect implementation type.</typeparam>
    /// <typeparam name="TAggregate">The aggregate state type this effect handles.</typeparam>
    /// <param name="builder">The Mississippi silo builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IMississippiSiloBuilder AddEventEffect<TEffect, TAggregate>(
        this IMississippiSiloBuilder builder
    )
        where TEffect : class, IEventEffect<TAggregate>
        where TAggregate : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ConfigureServices(services => AddEventEffectServices<TEffect, TAggregate>(services));
        return builder;
    }

    /// <summary>
    ///     Registers an event type so it can be resolved during aggregate hydration.
    /// </summary>
    /// <typeparam name="TEvent">
    ///     The event type. Must be decorated with <see cref="EventStorageNameAttribute" />.
    /// </typeparam>
    /// <param name="builder">The Mississippi silo builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IMississippiSiloBuilder AddEventType<TEvent>(
        this IMississippiSiloBuilder builder
    )
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddAggregateSupport();
        builder.ConfigureServices(services => AddEventTypeServices<TEvent>(services));
        return builder;
    }

    /// <summary>
    ///     Registers a fire-and-forget event effect for processing events on a specific aggregate type.
    /// </summary>
    /// <typeparam name="TEffect">The effect implementation type.</typeparam>
    /// <typeparam name="TEvent">The event type this effect handles.</typeparam>
    /// <typeparam name="TAggregate">The aggregate state type.</typeparam>
    /// <param name="builder">The Mississippi silo builder.</param>
    /// <returns>The builder for chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         Fire-and-forget effects run in a separate worker grain and do not block the aggregate.
    ///         The aggregate dispatches the effect and continues immediately without waiting for completion.
    ///     </para>
    ///     <para>
    ///         Unlike synchronous effects, fire-and-forget effects CANNOT yield additional events.
    ///         If the effect needs to trigger further state changes, it should send commands through
    ///         the normal aggregate command API.
    ///     </para>
    /// </remarks>
    public static IMississippiSiloBuilder AddFireAndForgetEventEffect<TEffect, TEvent, TAggregate>(
        this IMississippiSiloBuilder builder
    )
        where TEffect : class, IFireAndForgetEventEffect<TEvent, TAggregate>
        where TEvent : class
        where TAggregate : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ConfigureServices(services =>
            AddFireAndForgetEventEffectServices<TEffect, TEvent, TAggregate>(services));
        return builder;
    }

    /// <summary>
    ///     Adds a root command handler for the specified state type.
    /// </summary>
    /// <typeparam name="TSnapshot">The aggregate state type.</typeparam>
    /// <param name="builder">The Mississippi silo builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IMississippiSiloBuilder AddRootCommandHandler<TSnapshot>(
        this IMississippiSiloBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ConfigureServices(services => AddRootCommandHandlerServices<TSnapshot>(services));
        return builder;
    }

    /// <summary>
    ///     Adds a root event effect dispatcher for the specified aggregate type.
    /// </summary>
    /// <typeparam name="TAggregate">The aggregate state type.</typeparam>
    /// <param name="builder">The Mississippi silo builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IMississippiSiloBuilder AddRootEventEffect<TAggregate>(
        this IMississippiSiloBuilder builder
    )
        where TAggregate : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ConfigureServices(services => AddRootEventEffectServices<TAggregate>(services));
        return builder;
    }

    /// <summary>
    ///     Registers a snapshot type so it can be resolved during snapshot loading.
    /// </summary>
    /// <typeparam name="TSnapshot">
    ///     The snapshot type. Must be decorated with <see cref="SnapshotStorageNameAttribute" />.
    /// </typeparam>
    /// <param name="builder">The Mississippi silo builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IMississippiSiloBuilder AddSnapshotType<TSnapshot>(
        this IMississippiSiloBuilder builder
    )
        where TSnapshot : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddAggregateSupport();
        builder.ConfigureServices(services => AddSnapshotTypeServices<TSnapshot>(services));
        return builder;
    }

    /// <summary>
    ///     Scans an assembly for types decorated with <see cref="EventStorageNameAttribute" /> and registers them.
    /// </summary>
    /// <param name="builder">The Mississippi silo builder.</param>
    /// <param name="assembly">The assembly to scan for event types.</param>
    /// <returns>The builder for chaining.</returns>
    /// <remarks>
    ///     This method queues the assembly to be scanned when the <see cref="IEventTypeRegistry" /> is created.
    ///     All types in the assembly decorated with <see cref="EventStorageNameAttribute" /> will be automatically
    ///     registered, eliminating the need to call <see cref="AddEventType{TEvent}" /> for each type.
    /// </remarks>
    public static IMississippiSiloBuilder ScanAssemblyForEventTypes(
        this IMississippiSiloBuilder builder,
        Assembly assembly
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(assembly);
        builder.AddAggregateSupport();
        builder.ConfigureServices(services => services.AddSingleton(new AssemblyRegistration(assembly)));
        return builder;
    }

    /// <summary>
    ///     Scans the assembly containing the specified type for event types.
    /// </summary>
    /// <typeparam name="TMarker">A type in the assembly to scan.</typeparam>
    /// <param name="builder">The Mississippi silo builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IMississippiSiloBuilder ScanAssemblyForEventTypes<TMarker>(
        this IMississippiSiloBuilder builder
    ) =>
        builder.ScanAssemblyForEventTypes(typeof(TMarker).Assembly);

    /// <summary>
    ///     Scans an assembly for types decorated with <see cref="SnapshotStorageNameAttribute" /> and registers them.
    /// </summary>
    /// <param name="builder">The Mississippi silo builder.</param>
    /// <param name="assembly">The assembly to scan for snapshot types.</param>
    /// <returns>The builder for chaining.</returns>
    /// <remarks>
    ///     This method queues the assembly to be scanned when the <see cref="ISnapshotTypeRegistry" /> is created.
    ///     All types in the assembly decorated with <see cref="SnapshotStorageNameAttribute" /> will be automatically
    ///     registered, eliminating the need to call <see cref="AddSnapshotType{TSnapshot}" /> for each type.
    /// </remarks>
    public static IMississippiSiloBuilder ScanAssemblyForSnapshotTypes(
        this IMississippiSiloBuilder builder,
        Assembly assembly
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(assembly);
        builder.AddAggregateSupport();
        builder.ConfigureServices(services => services.AddSingleton(new SnapshotAssemblyRegistration(assembly)));
        return builder;
    }

    /// <summary>
    ///     Scans the assembly containing the specified type for snapshot types.
    /// </summary>
    /// <typeparam name="TMarker">A type in the assembly to scan.</typeparam>
    /// <param name="builder">The Mississippi silo builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IMississippiSiloBuilder ScanAssemblyForSnapshotTypes<TMarker>(
        this IMississippiSiloBuilder builder
    ) =>
        builder.ScanAssemblyForSnapshotTypes(typeof(TMarker).Assembly);

    private static void AddAggregateSupportServices(
        IServiceCollection services
    )
    {
        services.TryAddSingleton<IEventTypeRegistry>(provider =>
        {
            EventTypeRegistry registry = new();

            // Eagerly register all event types contributed via AddEventType<TEvent>().
            foreach (IEventTypeRegistration registration in provider.GetServices<IEventTypeRegistration>())
            {
                registration.Register(registry);
            }

            // Scan assemblies contributed via AddEventTypeAssembly().
            foreach (AssemblyRegistration assemblyRegistration in provider.GetServices<AssemblyRegistration>())
            {
                registry.ScanAssembly(assemblyRegistration.Assembly);
            }

            return registry;
        });
        services.TryAddSingleton<ISnapshotTypeRegistry>(provider =>
        {
            SnapshotTypeRegistry registry = new();

            // Eagerly register all snapshot types contributed via AddSnapshotType<TSnapshot>().
            foreach (ISnapshotTypeRegistration registration in provider.GetServices<ISnapshotTypeRegistration>())
            {
                registration.Register(registry);
            }

            // Scan assemblies contributed via ScanAssemblyForSnapshotTypes().
            foreach (SnapshotAssemblyRegistration assemblyRegistration in provider
                         .GetServices<SnapshotAssemblyRegistration>())
            {
                registry.ScanAssembly(assemblyRegistration.Assembly);
            }

            return registry;
        });
        services.TryAddTransient<IBrookEventConverter, BrookEventConverter>();
        services.TryAddTransient<IAggregateGrainFactory, AggregateGrainFactory>();
        services.TryAddSingleton(TimeProvider.System);
    }

    private static void AddCommandHandlerServices<TCommand, TSnapshot, THandler>(
        IServiceCollection services
    )
        where THandler : class, ICommandHandler<TCommand, TSnapshot>
    {
        services.AddTransient<ICommandHandler<TSnapshot>, THandler>();
        services.AddTransient<ICommandHandler<TCommand, TSnapshot>, THandler>();
        AddRootCommandHandlerServices<TSnapshot>(services);
    }

    private static void AddCommandHandlerServices<TCommand, TSnapshot>(
        IServiceCollection services,
        Func<TCommand, TSnapshot?, OperationResult<IReadOnlyList<object>>> handler
    )
    {
        services.AddTransient<ICommandHandler<TSnapshot>>(_ =>
            new DelegateCommandHandler<TCommand, TSnapshot>(handler));
        services.AddTransient<ICommandHandler<TCommand, TSnapshot>>(_ =>
            new DelegateCommandHandler<TCommand, TSnapshot>(handler));
        AddRootCommandHandlerServices<TSnapshot>(services);
    }

    private static void AddEventEffectServices<TEffect, TAggregate>(
        IServiceCollection services
    )
        where TEffect : class, IEventEffect<TAggregate>
        where TAggregate : class
    {
        services.AddTransient<IEventEffect<TAggregate>, TEffect>();
        AddRootEventEffectServices<TAggregate>(services);
    }

    private static void AddEventTypeServices<TEvent>(
        IServiceCollection services
    )
        where TEvent : class
    {
        services.AddSingleton<IEventTypeRegistration>(new EventTypeRegistration<TEvent>());
    }

    private static void AddFireAndForgetEventEffectServices<TEffect, TEvent, TAggregate>(
        IServiceCollection services
    )
        where TEffect : class, IFireAndForgetEventEffect<TEvent, TAggregate>
        where TEvent : class
        where TAggregate : class
    {
        // Register the effect itself (transient - new instance per execution)
        services.AddTransient<IFireAndForgetEventEffect<TEvent, TAggregate>, TEffect>();

        // Register a typed registration entry for discovery by the aggregate grain
        services.AddSingleton<IFireAndForgetEffectRegistration<TAggregate>>(
            new FireAndForgetEffectRegistration<TEffect, TEvent, TAggregate>());
    }

    private static void AddRootCommandHandlerServices<TSnapshot>(
        IServiceCollection services
    )
    {
        services.TryAddTransient<IRootCommandHandler<TSnapshot>, RootCommandHandler<TSnapshot>>();
    }

    private static void AddRootEventEffectServices<TAggregate>(
        IServiceCollection services
    )
        where TAggregate : class
    {
        services.TryAddTransient<IRootEventEffect<TAggregate>, RootEventEffect<TAggregate>>();
    }

    private static void AddSnapshotTypeServices<TSnapshot>(
        IServiceCollection services
    )
        where TSnapshot : class
    {
        services.AddSingleton<ISnapshotTypeRegistration>(new SnapshotTypeRegistration<TSnapshot>());
    }

    /// <summary>
    ///     Marker interface for event type registrations.
    /// </summary>
    internal interface IEventTypeRegistration
    {
        /// <summary>
        ///     Registers the event type with the registry.
        /// </summary>
        /// <param name="registry">The event type registry.</param>
        void Register(
            IEventTypeRegistry registry
        );
    }

    /// <summary>
    ///     Marker interface for snapshot type registrations.
    /// </summary>
    internal interface ISnapshotTypeRegistration
    {
        /// <summary>
        ///     Registers the snapshot type with the registry.
        /// </summary>
        /// <param name="registry">The snapshot type registry.</param>
        void Register(
            ISnapshotTypeRegistry registry
        );
    }

    /// <summary>
    ///     A marker record used to queue assemblies for event type scanning during startup.
    /// </summary>
    /// <param name="Assembly">The assembly to scan for event types.</param>
    private sealed record AssemblyRegistration(Assembly Assembly);

    /// <summary>
    ///     Implementation of event type registration for a specific event type.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    private sealed class EventTypeRegistration<TEvent> : IEventTypeRegistration
        where TEvent : class
    {
        /// <inheritdoc />
        public void Register(
            IEventTypeRegistry registry
        )
        {
            string eventName = EventStorageNameHelper.GetStorageName<TEvent>();
            registry.Register(eventName, typeof(TEvent));
        }
    }

    /// <summary>
    ///     A marker record used to queue assemblies for snapshot type scanning during startup.
    /// </summary>
    /// <param name="Assembly">The assembly to scan for snapshot types.</param>
    private sealed record SnapshotAssemblyRegistration(Assembly Assembly);

    /// <summary>
    ///     Implementation of snapshot type registration for a specific snapshot type.
    /// </summary>
    /// <typeparam name="TSnapshot">The snapshot type.</typeparam>
    private sealed class SnapshotTypeRegistration<TSnapshot> : ISnapshotTypeRegistration
        where TSnapshot : class
    {
        /// <inheritdoc />
        public void Register(
            ISnapshotTypeRegistry registry
        )
        {
            string snapshotName = SnapshotStorageNameHelper.GetStorageName<TSnapshot>();
            registry.Register(snapshotName, typeof(TSnapshot));
        }
    }
}