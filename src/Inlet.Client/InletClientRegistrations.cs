using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.Inlet.Client.Abstractions;
using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Core;


namespace Mississippi.Inlet.Client;

/// <summary>
///     Extension methods for registering Inlet services.
/// </summary>
public static class InletClientRegistrations
{
    /// <summary>
    ///     Adds Inlet client services to the Reservoir builder.
    /// </summary>
    /// <param name="reservoir">The Reservoir builder.</param>
    /// <returns>The Reservoir builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="reservoir" /> is null.</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="reservoir" /> is not backed by the current Reservoir builder implementation.
    /// </exception>
    public static IReservoirBuilder AddInletClient(
        this IReservoirBuilder reservoir
    )
    {
        ArgumentNullException.ThrowIfNull(reservoir);
        ReservoirBuilder builder = reservoir as ReservoirBuilder ??
                                   throw new ArgumentException(
                                       "The provided reservoir builder is not supported by the current Inlet client implementation.",
                                       nameof(reservoir));
        builder.Services.TryAddSingleton<IProjectionRegistry, ProjectionRegistry>();
        builder.Services.TryAddScoped<IInletStore>(sp => new CompositeInletStore(sp.GetRequiredService<IStore>()));
        builder.Services.TryAddScoped<IProjectionUpdateNotifier>(sp =>
            new ProjectionNotifier(sp.GetRequiredService<IStore>()));
        return reservoir;
    }

    /// <summary>
    ///     Registers a projection path on the Reservoir builder.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="reservoir">The Reservoir builder.</param>
    /// <param name="path">The projection path.</param>
    /// <returns>The Reservoir builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="reservoir" /> or <paramref name="path" /> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="reservoir" /> is not backed by the current Reservoir builder implementation.
    /// </exception>
    public static IReservoirBuilder AddProjectionPath<T>(
        this IReservoirBuilder reservoir,
        string path
    )
        where T : class
    {
        ArgumentNullException.ThrowIfNull(reservoir);
        ArgumentNullException.ThrowIfNull(path);
        ReservoirBuilder builder = reservoir as ReservoirBuilder ??
                                   throw new ArgumentException(
                                       "The provided reservoir builder is not supported by the current Inlet client implementation.",
                                       nameof(reservoir));
        builder.Services.AddSingleton<IConfigureProjectionRegistry>(new ProjectionPathRegistration<T>(path));
        return reservoir;
    }

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
    ///             <item><see cref="IInletStore" /> - Composite interface for components</item>
    ///             <item><see cref="IProjectionUpdateNotifier" /> - For dispatching projection updates</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Use scoped lifetime to match Fluxor pattern:
    ///         Blazor WASM: scoped = singleton (no difference);
    ///         Blazor Server: scoped = per-circuit (each user gets own store).
    ///     </para>
    /// </remarks>
    internal static IServiceCollection AddInletClient(
        this IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ReservoirBuilder reservoirBuilder = new(services);
        reservoirBuilder.AddInletClient();
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
    internal static IServiceCollection AddProjectionPath<T>(
        this IServiceCollection services,
        string path
    )
        where T : class
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(path);
        ReservoirBuilder reservoirBuilder = new(services);
        reservoirBuilder.AddProjectionPath<T>(path);
        return services;
    }
}