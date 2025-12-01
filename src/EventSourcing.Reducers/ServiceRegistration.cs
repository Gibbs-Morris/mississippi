using System;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Mississippi.EventSourcing.Reducers;

/// <summary>
///     Provides helper methods for registering reducer components with the dependency injection container.
/// </summary>
public static class ServiceRegistration
{
    /// <summary>
    ///     Registers the default reducer infrastructure for the EventSourcing reducers feature.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddEventSourcingReducers(
        this IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.AddEventSourcingReducersCore();
    }

    /// <summary>
    ///     Registers a reducer type for its implemented <see cref="IReducer{TModel}" /> interface.
    /// </summary>
    /// <typeparam name="TReducer">The reducer implementation type.</typeparam>
    /// <typeparam name="TModel">The model type handled by the reducer.</typeparam>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddReducer<TReducer, TModel>(
        this IServiceCollection services
    )
        where TModel : class
        where TReducer : class, IReducer<TModel>
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.AddSingleton<IReducer<TModel>, TReducer>();
    }

    /// <summary>
    ///     Registers a reducer for every <see cref="IReducer{TModel}" /> interface implemented by the reducer type.
    /// </summary>
    /// <typeparam name="TReducer">The reducer implementation type.</typeparam>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the reducer type does not implement
    ///     <see cref="IReducer{TModel}" />.
    /// </exception>
    public static IServiceCollection AddReducer<TReducer>(
        this IServiceCollection services
    )
        where TReducer : class
    {
        ArgumentNullException.ThrowIfNull(services);
        Type reducerType = typeof(TReducer);
        bool registered = false;
        foreach (Type reducerInterface in reducerType.GetInterfaces().Where(IsReducerInterface))
        {
            services.AddSingleton(reducerInterface, reducerType);
            registered = true;
        }

        if (!registered)
        {
            throw new InvalidOperationException(
                $"Type '{reducerType.FullName}' does not implement '{typeof(IReducer<>).Name}'.");
        }

        return services;
    }

    /// <summary>
    ///     Registers the default <see cref="IRootReducer{TModel}" /> implementation as an open generic so any model type is
    ///     resolved.
    /// </summary>
    /// <typeparam name="TModel">The model type produced by the reducers.</typeparam>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddRootReducer<TModel>(
        this IServiceCollection services
    )
        where TModel : class
    {
        // Maintain the legacy API while delegating to the feature-level registration method.
        services.AddEventSourcingReducers();
        services.TryAddSingleton<IRootReducer<TModel>, RootReducer<TModel>>();
        return services;
    }

    private static IServiceCollection AddEventSourcingReducersCore(
        this IServiceCollection services
    )
    {
        services.TryAddSingleton(typeof(IRootReducer<>), typeof(RootReducer<>));
        return services;
    }

    private static bool IsReducerInterface(
        Type interfaceType
    ) =>
        interfaceType.IsGenericType && (interfaceType.GetGenericTypeDefinition() == typeof(IReducer<>));
}