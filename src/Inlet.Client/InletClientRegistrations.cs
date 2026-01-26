using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.Inlet.Abstractions;
using Mississippi.Reservoir;
using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Inlet.Client;

/// <summary>
///     Extension methods for registering Inlet services.
/// </summary>
public static class InletClientRegistrations
{
    /// <summary>
    ///     Adds Inlet services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services" /> is null.</exception>
    /// <remarks>
    ///     <para>
    ///         This method registers the following services:
    ///         <list type="bullet">
    ///             <item><see cref="IProjectionCache" /> - Thread-safe cache for projection states</item>
    ///             <item><see cref="IStore" /> - Redux-style state container</item>
    ///             <item><see cref="IInletStore" /> - Composite interface for components</item>
    ///             <item><see cref="IProjectionUpdateNotifier" /> - For pushing projection updates</item>
    ///             <item><see cref="ProjectionCacheMiddleware" /> - Intercepts projection actions</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Use scoped lifetime to match Fluxor pattern:
    ///         Blazor WASM: scoped = singleton (no difference);
    ///         Blazor Server: scoped = per-circuit (each user gets own store).
    ///     </para>
    /// </remarks>
    public static IServiceCollection AddInletClient(
        this IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<IProjectionRegistry, ProjectionRegistry>();

        // Register the projection cache (scoped per circuit/user)
        services.TryAddScoped<IProjectionCache, ProjectionCache>();

        // Register the projection cache middleware to intercept projection actions
        services.AddScoped<IMiddleware, ProjectionCacheMiddleware>();

        // Register the Store with DI-resolved components
        services.TryAddScoped<IStore>(sp => new Store(
            sp.GetServices<IFeatureStateRegistration>(),
            sp.GetServices<IMiddleware>()));

        // Register the composite InletStore for backward compatibility
        services.TryAddScoped<IInletStore>(sp => new CompositeInletStore(
            sp.GetRequiredService<IStore>(),
            sp.GetRequiredService<IProjectionCache>()));

        // Register the projection notifier for pushing updates
        services.TryAddScoped<IProjectionUpdateNotifier>(sp => new ProjectionNotifier(
            sp.GetRequiredService<IStore>(),
            sp.GetRequiredService<IProjectionCache>()));
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
