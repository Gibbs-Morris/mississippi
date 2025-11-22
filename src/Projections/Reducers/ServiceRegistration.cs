using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;


namespace Mississippi.Projections.Reducers;

/// <summary>
///     Provides dependency-injection helpers for registering projection reducers.
/// </summary>
public static class ServiceRegistration
{
    /// <summary>
    ///     Registers the core reducer infrastructure so that reducers and root reducers can be injected.
    /// </summary>
    /// <param name="services">The service collection to modify.</param>
    /// <returns>The original service collection for chaining.</returns>
    public static IServiceCollection AddProjectionReducers(
        this IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.AddRootReducer();
    }

    /// <summary>
    ///     Registers a reducer implementation as a singleton for the specified model type.
    /// </summary>
    /// <typeparam name="TModel">The projection model type the reducer targets.</typeparam>
    /// <typeparam name="TReducer">The reducer implementation type.</typeparam>
    /// <param name="services">The service collection to modify.</param>
    /// <returns>The original service collection for chaining.</returns>
    public static IServiceCollection AddReducer<TModel, TReducer>(
        this IServiceCollection services
    )
        where TModel : notnull, new()
        where TReducer : class, IReducer<TModel>
    {
        ArgumentNullException.ThrowIfNull(services);
        _ = services.AddRootReducer();
        services.AddSingleton<IReducer<TModel>, TReducer>();
        return services;
    }

    /// <summary>
    ///     Registers the open generic <see cref="IRootReducer{TModel}" /> so any projection model can resolve a root reducer
    ///     at runtime.
    /// </summary>
    /// <param name="services">The service collection to modify.</param>
    /// <returns>The original service collection for chaining.</returns>
    public static IServiceCollection AddRootReducer(
        this IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton(typeof(IRootReducer<>), typeof(RootReducer<>));
        return services;
    }
}