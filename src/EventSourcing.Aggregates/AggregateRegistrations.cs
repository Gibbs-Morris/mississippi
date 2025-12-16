using System;
using System.Collections.Generic;

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
        services.TryAddSingleton<IEventTypeRegistry, EventTypeRegistry>();
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
        services.AddTransient<ICommandHandler<TCommand, TState>, THandler>();
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
        services.AddTransient<ICommandHandler<TCommand, TState>>(_ =>
            new DelegateCommandHandler<TCommand, TState>(handler));
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