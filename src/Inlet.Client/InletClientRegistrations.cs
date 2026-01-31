using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.Inlet.Client.Abstractions;
using Mississippi.Inlet.Client.Abstractions.State;
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
    ///             <item><see cref="IStore" /> - Redux-style state container</item>
    ///             <item><see cref="IInletStore" /> - Composite interface for components</item>
    ///             <item><see cref="IProjectionUpdateNotifier" /> - For dispatching projection updates</item>
    ///             <item><see cref="ProjectionsFeatureState" /> - Feature state for all projections</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Use scoped lifetime to match Fluxor pattern:
    ///         Blazor WASM: scoped = singleton (no difference);
    ///         Blazor Server: scoped = per-circuit (each user gets own store).
    ///     </para>
    ///     <para>
    ///         Projection state is stored in <see cref="ProjectionsFeatureState" /> and follows the
    ///         Redux pattern. Access via <c>store.GetState&lt;ProjectionsFeatureState&gt;()</c>.
    ///     </para>
    /// </remarks>
    public static IServiceCollection AddInletClient(
        this IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<IProjectionRegistry, ProjectionRegistry>();

        // Register the projections feature state
        services.AddFeatureState<ProjectionsFeatureState>();

        // Register TimeProvider if not already registered
        services.TryAddSingleton(TimeProvider.System);

        // Register the Store with DI-resolved components
        services.TryAddScoped<IStore>(sp => new Store(
            sp.GetServices<IFeatureStateRegistration>(),
            sp.GetServices<IMiddleware>(),
            sp.GetRequiredService<TimeProvider>()));

        // Register the composite InletStore (wraps Store)
        services.TryAddScoped<IInletStore>(sp => new CompositeInletStore(sp.GetRequiredService<IStore>()));

        // Register the projection notifier for dispatching updates
        services.TryAddScoped<IProjectionUpdateNotifier>(sp => new ProjectionNotifier(sp.GetRequiredService<IStore>()));
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