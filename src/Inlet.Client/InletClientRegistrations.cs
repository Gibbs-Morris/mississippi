using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.Inlet.Client.Abstractions;
using Mississippi.Inlet.Client.Abstractions.State;
using Mississippi.Reservoir.Abstractions;


namespace Mississippi.Inlet.Client;

/// <summary>
///     Extension methods for registering Inlet services.
/// </summary>
public static class InletClientRegistrations
{
    /// <summary>
    ///     Adds Inlet services to the service collection.
    /// </summary>
    /// <param name="builder">The Reservoir builder.</param>
    /// <returns>The builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder" /> is null.</exception>
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
    public static IReservoirBuilder AddInletClient(
        this IReservoirBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        IServiceCollection services = builder.Services;
        services.TryAddSingleton<IProjectionRegistry, ProjectionRegistry>();
        builder.AddFeatureState<ProjectionsFeatureState>();
        services.TryAddScoped<IInletStore>(sp => new CompositeInletStore(sp.GetRequiredService<IStore>()));
        services.TryAddScoped<IProjectionUpdateNotifier>(sp => new ProjectionNotifier(sp.GetRequiredService<IStore>()));
        return builder;
    }

    /// <summary>
    ///     Registers a projection path.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="builder">The Reservoir builder.</param>
    /// <param name="path">The projection path.</param>
    /// <returns>The builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="builder" /> or <paramref name="path" /> is null.
    /// </exception>
    public static IReservoirBuilder AddProjectionPath<T>(
        this IReservoirBuilder builder,
        string path
    )
        where T : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(path);
        builder.Services.AddSingleton<IConfigureProjectionRegistry>(new ProjectionPathRegistration<T>(path));
        return builder;
    }
}