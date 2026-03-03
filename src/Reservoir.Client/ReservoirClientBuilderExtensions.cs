using System;

using Mississippi.Common.Builders.Client.Abstractions;
using Mississippi.Reservoir.Builder;


namespace Mississippi.Reservoir.Client;

/// <summary>
///     Client-builder extension methods for Reservoir client features.
/// </summary>
public static class ReservoirClientBuilderExtensions
{
    /// <summary>
    ///     Adds Reservoir DevTools integration.
    /// </summary>
    /// <param name="builder">Client builder.</param>
    /// <param name="configure">Optional options configuration.</param>
    /// <returns>The same client builder instance for fluent chaining.</returns>
    public static IClientBuilder AddDevTools(
        this IClientBuilder builder,
        Action<ReservoirDevToolsOptions>? configure = null
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddReservoirDevTools(configure);
        return builder;
    }

    /// <summary>
    ///     Adds Reservoir built-in Blazor features (Navigation and Lifecycle)
    ///     with core Reservoir store infrastructure.
    /// </summary>
    /// <param name="builder">Client builder.</param>
    /// <returns>The same client builder instance for fluent chaining.</returns>
    public static IClientBuilder AddReservoirBuiltIns(
        this IClientBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddReservoir(r => r.AddBuiltIns());
        return builder;
    }
}