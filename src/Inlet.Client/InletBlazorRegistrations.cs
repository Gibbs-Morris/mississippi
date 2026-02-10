using System;

using Mississippi.Inlet.Client.ActionEffects;
using Mississippi.Reservoir.Abstractions.Builders;


namespace Mississippi.Inlet.Client;

/// <summary>
///     Extension methods for adding Inlet Blazor services.
/// </summary>
public static class InletBlazorRegistrations
{
    /// <summary>
    ///     Adds Inlet Blazor services to the Reservoir builder.
    /// </summary>
    /// <param name="builder">The Reservoir builder.</param>
    /// <returns>The Reservoir builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder" /> is null.</exception>
    /// <remarks>
    ///     This must be called after <c>AddInlet()</c>.
    /// </remarks>
    public static IReservoirBuilder AddInletBlazor(
        this IReservoirBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder;
    }

    /// <summary>
    ///     Adds Inlet Blazor SignalR services to the Reservoir builder.
    /// </summary>
    /// <param name="builder">The Reservoir builder.</param>
    /// <param name="configure">Optional configuration for the builder.</param>
    /// <returns>The Reservoir builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder" /> is null.</exception>
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
    public static IReservoirBuilder AddInletBlazorSignalR(
        this IReservoirBuilder builder,
        Action<InletBlazorSignalRBuilder>? configure = null
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        InletBlazorSignalRBuilder signalRBuilder = new(builder);
        configure?.Invoke(signalRBuilder);
        signalRBuilder.Build();
        return builder;
    }
}