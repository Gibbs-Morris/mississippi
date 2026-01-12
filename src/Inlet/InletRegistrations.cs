using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.Inlet.Abstractions;
using Mississippi.Reservoir;
using Mississippi.Reservoir.Abstractions;


namespace Mississippi.Inlet;

/// <summary>
///     Extension methods for registering Inlet services.
/// </summary>
public static class InletRegistrations
{
    /// <summary>
    ///     Adds Inlet services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services" /> is null.</exception>
    public static IServiceCollection AddInlet(
        this IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.AddInlet(null);
    }

    /// <summary>
    ///     Adds Inlet services to the service collection with store configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureStore">
    ///     Action to configure the store with feature states.
    ///     This is called each time a new store instance is created.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="services" /> is null.
    /// </exception>
    public static IServiceCollection AddInlet(
        this IServiceCollection services,
        Action<Store>? configureStore
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<IProjectionRegistry, ProjectionRegistry>();

        // Use scoped lifetime to match Fluxor pattern:
        // - Blazor WASM: scoped = singleton (no difference)
        // - Blazor Server: scoped = per-circuit (each user gets own store)
        services.TryAddScoped(sp =>
        {
            InletStore store = new(sp);
            configureStore?.Invoke(store);
            return store;
        });
        services.TryAddScoped<IStore>(sp => sp.GetRequiredService<InletStore>());
        services.TryAddScoped<IInletStore>(sp => sp.GetRequiredService<InletStore>());
        services.TryAddScoped<IProjectionUpdateNotifier>(sp => sp.GetRequiredService<InletStore>());
        return services;
    }

    /// <summary>
    ///     Registers a projection path.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="path">The projection path.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="services" /> or <paramref name="path" /> is null.
    /// </exception>
    public static IServiceCollection AddProjectionPath<T>(
        this IServiceCollection services,
        string path
    )
        where T : class
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(path);
        services.AddSingleton<IConfigureProjectionRegistry>(new ProjectionPathRegistration<T>(path));
        return services;
    }
}