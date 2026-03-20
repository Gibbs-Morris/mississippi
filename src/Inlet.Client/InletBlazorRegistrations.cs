using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Inlet.Client.ActionEffects;
using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Core;


namespace Mississippi.Inlet.Client;

/// <summary>
///     Extension methods for adding Inlet Blazor services.
/// </summary>
public static class InletBlazorRegistrations
{
    /// <summary>
    ///     Adds Inlet Blazor services to the Reservoir builder.
    /// </summary>
    /// <param name="reservoir">The Reservoir builder.</param>
    /// <returns>The Reservoir builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="reservoir" /> is null.</exception>
    public static IReservoirBuilder AddInletBlazor(
        this IReservoirBuilder reservoir
    )
    {
        ArgumentNullException.ThrowIfNull(reservoir);
        return reservoir;
    }

    /// <summary>
    ///     Adds Inlet Blazor SignalR services to the Reservoir builder.
    /// </summary>
    /// <param name="reservoir">The Reservoir builder.</param>
    /// <param name="configure">Optional configuration for the builder.</param>
    /// <returns>The Reservoir builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="reservoir" /> is null.</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="reservoir" /> is not backed by the current Reservoir builder implementation.
    /// </exception>
    public static IReservoirBuilder AddInletBlazorSignalR(
        this IReservoirBuilder reservoir,
        Action<InletBlazorSignalRBuilder>? configure = null
    )
    {
        ArgumentNullException.ThrowIfNull(reservoir);
        ReservoirBuilder builder = reservoir as ReservoirBuilder ??
                                   throw new ArgumentException(
                                       "The provided reservoir builder is not supported by the current Inlet client implementation.",
                                       nameof(reservoir));
        InletBlazorSignalRBuilder signalRBuilder = new(builder.Services);
        configure?.Invoke(signalRBuilder);
        signalRBuilder.Build();
        return reservoir;
    }

    /// <summary>
    ///     Adds Inlet Blazor services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services" /> is null.</exception>
    /// <remarks>
    ///     This must be called after <c>AddInlet()</c>.
    /// </remarks>
    internal static IServiceCollection AddInletBlazor(
        this IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }

    /// <summary>
    ///     Adds Inlet Blazor SignalR services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration for the builder.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services" /> is null.</exception>
    /// <remarks>
    ///     <para>
    ///         This registers the <see cref="InletSignalRActionEffect" /> which handles projection
    ///         subscription actions via SignalR. The effect requires an <see cref="IProjectionFetcher" />
    ///         to be registered.
    ///     </para>
    ///     <para>
    ///         Use the builder to configure the hub path and register projection fetchers.
    ///     </para>
    /// </remarks>
    internal static IServiceCollection AddInletBlazorSignalR(
        this IServiceCollection services,
        Action<InletBlazorSignalRBuilder>? configure = null
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ReservoirBuilder reservoirBuilder = new(services);
        reservoirBuilder.AddInletBlazorSignalR(configure);
        return services;
    }
}