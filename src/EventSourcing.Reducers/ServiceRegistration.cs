using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Mississippi.EventSourcing.Reducers;

/// <summary>
///     Provides extension methods for registering event sourcing reducer services.
/// </summary>
public static class ServiceRegistration
{
    /// <summary>
    ///     Registers a root reducer for the specified model type.
    /// </summary>
    /// <typeparam name="TModel">The type of the model being reduced.</typeparam>
    /// <param name="services">The service collection to register with.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <remarks>
    ///     Registers <see cref="RootReducer{TModel}" /> as the implementation of <see cref="IRootReducer{TModel}" />.
    ///     This should be called after all individual reducers have been registered.
    /// </remarks>
    public static IServiceCollection AddRootReducer<TModel>(
        this IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IRootReducer<TModel>, RootReducer<TModel>>();
        return services;
    }

    /// <summary>
    ///     Registers a reducer for the specified model and event types.
    /// </summary>
    /// <typeparam name="TModel">The type of the model being reduced.</typeparam>
    /// <typeparam name="TEvent">The type of the event the reducer handles.</typeparam>
    /// <typeparam name="TReducer">The reducer implementation type.</typeparam>
    /// <param name="services">The service collection to register with.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <remarks>
    ///     Registers the reducer as a singleton that can be composed into a root reducer.
    /// </remarks>
    public static IServiceCollection AddReducer<TModel, TEvent, TReducer>(
        this IServiceCollection services
    )
        where TReducer : class, IReducer<TModel, TEvent>
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register the strongly-typed reducer interface
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IReducer<TModel, TEvent>, TReducer>());

        // Also register as object-based reducer for root reducer composition
        // Use Add (not TryAddEnumerable) to allow multiple adapters for different event types
        services.Add(ServiceDescriptor.Singleton<IReducer<TModel, object>>(provider =>
        {
            IReducer<TModel, TEvent> typedReducer = provider.GetRequiredService<IReducer<TModel, TEvent>>();
            return new ReducerAdapter<TModel, TEvent>(typedReducer);
        }));

        return services;
    }

    /// <summary>
    ///     Registers a reducer instance for the specified model and event types.
    /// </summary>
    /// <typeparam name="TModel">The type of the model being reduced.</typeparam>
    /// <typeparam name="TEvent">The type of the event the reducer handles.</typeparam>
    /// <param name="services">The service collection to register with.</param>
    /// <param name="reducer">The reducer instance to register.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <remarks>
    ///     Registers the provided reducer instance as a singleton.
    /// </remarks>
    public static IServiceCollection AddReducer<TModel, TEvent>(
        this IServiceCollection services,
        IReducer<TModel, TEvent> reducer
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(reducer);

        // Register the reducer instance
        services.Add(ServiceDescriptor.Singleton<IReducer<TModel, TEvent>>(reducer));

        // Also register as object-based reducer for root reducer composition
        services.Add(ServiceDescriptor.Singleton<IReducer<TModel, object>>(
            new ReducerAdapter<TModel, TEvent>(reducer)
        ));

        return services;
    }

    /// <summary>
    ///     Registers a reducer using a factory function.
    /// </summary>
    /// <typeparam name="TModel">The type of the model being reduced.</typeparam>
    /// <typeparam name="TEvent">The type of the event the reducer handles.</typeparam>
    /// <param name="services">The service collection to register with.</param>
    /// <param name="reducerFactory">A factory function that creates the reducer.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <remarks>
    ///     The factory function is called once to create a singleton reducer instance.
    /// </remarks>
    public static IServiceCollection AddReducer<TModel, TEvent>(
        this IServiceCollection services,
        Func<IServiceProvider, IReducer<TModel, TEvent>> reducerFactory
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(reducerFactory);

        // Register the strongly-typed reducer interface using factory
        services.Add(ServiceDescriptor.Singleton<IReducer<TModel, TEvent>>(reducerFactory));

        // Also register as object-based reducer for root reducer composition
        services.Add(ServiceDescriptor.Singleton<IReducer<TModel, object>>(provider =>
        {
            IReducer<TModel, TEvent> typedReducer = reducerFactory(provider);
            return new ReducerAdapter<TModel, TEvent>(typedReducer);
        }));

        return services;
    }

    /// <summary>
    ///     Internal adapter to convert typed reducers to object-based reducers.
    /// </summary>
    /// <summary>
    ///     Adapter that wraps a strongly-typed reducer to work with object-based events.
    /// </summary>
    /// <typeparam name="TModel">The type of the model being reduced.</typeparam>
    /// <typeparam name="TEvent">The type of the event the inner reducer handles.</typeparam>
    private sealed class ReducerAdapter<TModel, TEvent> : IReducer<TModel, object>
    {
        private IReducer<TModel, TEvent> InnerReducer { get; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ReducerAdapter{TModel, TEvent}" /> class.
        /// </summary>
        /// <param name="innerReducer">The typed reducer to wrap.</param>
        public ReducerAdapter(
            IReducer<TModel, TEvent> innerReducer
        )
        {
            InnerReducer = innerReducer ?? throw new ArgumentNullException(nameof(innerReducer));
        }

        /// <inheritdoc />
        public TModel Reduce(
            TModel model,
            object @event
        )
        {
            if (@event is TEvent typedEvent)
            {
                return InnerReducer.Reduce(model, typedEvent);
            }

            return model;
        }
    }
}
