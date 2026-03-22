using System;

using Mississippi.Inlet.Client.ActionEffects;
using Mississippi.Reservoir.Abstractions;


namespace Mississippi.Inlet.Client;

/// <summary>
///     Extension methods for composing Inlet Blazor features.
/// </summary>
public static class InletBlazorRegistrations
{
    /// <summary>
    ///     Returns the Reservoir builder for baseline Inlet Blazor composition.
    /// </summary>
    /// <param name="builder">The Reservoir builder.</param>
    /// <returns>The builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder" /> is null.</exception>
    /// <remarks>
    ///     This method is currently a no-op extension point. It performs no additional
    ///     registrations today and exists so callers can opt into future baseline Inlet Blazor
    ///     composition without changing their startup shape.
    /// </remarks>
    public static IReservoirBuilder AddInletBlazor(
        this IReservoirBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder;
    }

    /// <summary>
    ///     Adds Inlet Blazor SignalR services to the service collection.
    /// </summary>
    /// <param name="builder">The Reservoir builder.</param>
    /// <param name="configure">Optional configuration for the builder.</param>
    /// <returns>The builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder" /> is null.</exception>
    /// <remarks>
    ///     <para>
    ///         This registers the <see cref="InletSignalRActionEffect" /> which handles projection
    ///         subscription actions via SignalR. The effect requires an <see cref="IProjectionFetcher" />
    ///         to be registered.
    ///     </para>
    ///     <para>
    ///         This method also ensures the core Inlet client services are registered before the
    ///         SignalR feature is composed.
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
        builder.AddInletClient();
        InletBlazorSignalRBuilder signalRBuilder = new(builder);
        configure?.Invoke(signalRBuilder);
        signalRBuilder.Build();
        return builder;
    }
}