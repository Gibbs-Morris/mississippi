using System;
using System.Collections.Generic;
using System.Reflection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.EventSourcing.Abstractions.Attributes;
using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Mississippi.EventSourcing.Aggregates;

/// <summary>
///     Provides extension methods for registering aggregate components in the dependency injection container.
/// </summary>
public static class AggregateRegistrations
{
    /// <summary>
    ///     Adds aggregate infrastructure services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAggregateSupport(
        this IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
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
        services.TryAddTransient<IAggregateGrainFactory, AggregateGrainFactory>();
        return services;
    }

    /// <summary>
    ///     Registers a command handler for processing commands against aggregate state.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <typeparam name="TState">The aggregate state type.</typeparam>
    /// <typeparam name="THandler">The command handler implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCommandHandler<TCommand, TState, THandler>(
        this IServiceCollection services
    )
        where THandler : class, ICommandHandler<TCommand, TState>
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddTransient<ICommandHandler<TState>, THandler>();
        services.AddTransient<ICommandHandler<TCommand, TState>, THandler>();
        services.AddRootCommandHandler<TState>();
        return services;
    }

    /// <summary>
    ///     Registers a command handler using a delegate for processing commands against aggregate state.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <typeparam name="TState">The aggregate state type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="handler">The delegate that handles the command.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCommandHandler<TCommand, TState>(
        this IServiceCollection services,
        Func<TCommand, TState?, OperationResult<IReadOnlyList<object>>> handler
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(handler);
        services.AddTransient<ICommandHandler<TState>>(_ => new DelegateCommandHandler<TCommand, TState>(handler));
        services.AddTransient<ICommandHandler<TCommand, TState>>(_ =>
            new DelegateCommandHandler<TCommand, TState>(handler));
        services.AddRootCommandHandler<TState>();
        return services;
    }

    /// <summary>
    ///     Registers an event type so it can be resolved during aggregate hydration.
    /// </summary>
    /// <typeparam name="TEvent">
    ///     The event type. Must be decorated with <see cref="EventNameAttribute" />.
    /// </typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEventType<TEvent>(
        this IServiceCollection services
    )
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(services);

        // Ensure aggregate support is added
        services.AddAggregateSupport();

        // Register the event type at startup
        services.AddSingleton<IEventTypeRegistration>(new EventTypeRegistration<TEvent>());
        return services;
    }

    /// <summary>
    ///     Adds a root command handler for the specified state type.
    /// </summary>
    /// <typeparam name="TState">The aggregate state type.</typeparam>
    /// <param name="services">The service collection to add the root command handler to.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddRootCommandHandler<TState>(
        this IServiceCollection services
    )
    {
        services.TryAddTransient<IRootCommandHandler<TState>, RootCommandHandler<TState>>();
        return services;
    }

    /// <summary>
    ///     Scans an assembly for types decorated with <see cref="EventNameAttribute" /> and registers them.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assembly">The assembly to scan for event types.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    ///     This method queues the assembly to be scanned when the <see cref="IEventTypeRegistry" /> is created.
    ///     All types in the assembly decorated with <see cref="EventNameAttribute" /> will be automatically
    ///     registered, eliminating the need to call <see cref="AddEventType{TEvent}" /> for each type.
    /// </remarks>
    public static IServiceCollection ScanAssemblyForEventTypes(
        this IServiceCollection services,
        Assembly assembly
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);

        // Ensure aggregate support is added
        services.AddAggregateSupport();

        // Queue the assembly for scanning
        services.AddSingleton(new AssemblyRegistration(assembly));
        return services;
    }

    /// <summary>
    ///     Scans the assembly containing the specified type for event types.
    /// </summary>
    /// <typeparam name="TMarker">A type in the assembly to scan.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection ScanAssemblyForEventTypes<TMarker>(
        this IServiceCollection services
    ) =>
        services.ScanAssemblyForEventTypes(typeof(TMarker).Assembly);

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
            string eventName = EventNameHelper.GetEventName<TEvent>();
            registry.Register(eventName, typeof(TEvent));
        }
    }
}