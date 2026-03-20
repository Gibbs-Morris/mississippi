using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.State;
using Mississippi.Reservoir.Core.State;


namespace Mississippi.Reservoir.Core;

/// <summary>
///     Internal helpers for translating Reservoir builder configuration into service registrations.
/// </summary>
internal static class ReservoirServiceRegistrationHelpers
{
    /// <summary>
    ///     Adds feature-state registration services for the specified state type.
    /// </summary>
    /// <typeparam name="TState">The feature state type.</typeparam>
    /// <param name="services">The service collection.</param>
    internal static void AddFeatureState<TState>(
        IServiceCollection services
    )
        where TState : class, IFeatureState, new()
    {
        services.TryAddEnumerable(
            ServiceDescriptor.Scoped<IFeatureStateRegistration, FeatureStateRegistration<TState>>(sp => new(
                sp.GetService<IRootReducer<TState>>(),
                sp.GetService<IRootActionEffect<TState>>())));
    }

    /// <summary>
    ///     Adds the base Reservoir runtime services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    internal static void AddReservoirCoreServices(
        IServiceCollection services
    )
    {
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped<IStore>(sp => new Store(
            sp.GetServices<IFeatureStateRegistration>(),
            sp.GetServices<IMiddleware>(),
            sp.GetRequiredService<TimeProvider>()));
    }

    /// <summary>
    ///     Adds the root action effect adapter for the specified state type.
    /// </summary>
    /// <typeparam name="TState">The feature state type.</typeparam>
    /// <param name="services">The service collection.</param>
    internal static void AddRootActionEffect<TState>(
        IServiceCollection services
    )
        where TState : class, IFeatureState, new()
    {
        services.TryAddTransient<IRootActionEffect<TState>, RootActionEffect<TState>>();
    }

    /// <summary>
    ///     Adds the root reducer adapter for the specified state type.
    /// </summary>
    /// <typeparam name="TState">The feature state type.</typeparam>
    /// <param name="services">The service collection.</param>
    internal static void AddRootReducer<TState>(
        IServiceCollection services
    )
        where TState : class, IFeatureState, new()
    {
        services.TryAddTransient<IRootReducer<TState>, RootReducer<TState>>();
    }

    /// <summary>
    ///     Gets the supported concrete Reservoir builder implementation.
    /// </summary>
    /// <param name="reservoir">The public Reservoir builder contract.</param>
    /// <returns>The concrete Reservoir builder.</returns>
    internal static ReservoirBuilder GetBuilder(
        IReservoirBuilder reservoir
    ) =>
        reservoir as ReservoirBuilder ??
        throw new ArgumentException(
            "The provided reservoir builder is not supported by the current Reservoir.Core implementation.",
            nameof(reservoir));

    /// <summary>
    ///     Gets the supported concrete feature-state builder implementation.
    /// </summary>
    /// <typeparam name="TState">The feature state type.</typeparam>
    /// <param name="feature">The public feature-state builder contract.</param>
    /// <returns>The concrete feature-state builder.</returns>
    internal static ReservoirFeatureStateBuilder<TState> GetFeatureBuilder<TState>(
        IFeatureStateBuilder<TState> feature
    )
        where TState : class, IFeatureState, new() =>
        feature as ReservoirFeatureStateBuilder<TState> ??
        throw new ArgumentException(
            $"The provided feature builder for '{typeof(TState).FullName}' is not supported by the current Reservoir.Core implementation.",
            nameof(feature));
}