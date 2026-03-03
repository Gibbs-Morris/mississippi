using System;

using Mississippi.Common.Builders.Client.Abstractions;
using Mississippi.Reservoir.Core;


namespace Mississippi.Reservoir.Builder;

/// <summary>
///     Client-builder extension methods for Reservoir registration.
/// </summary>
public static class ReservoirClientBuilderExtensions
{
    /// <summary>
    ///     Adds Reservoir store infrastructure with optional builder-based configuration.
    ///     Should be called exactly once per service collection; reducers are not idempotent.
    /// </summary>
    /// <param name="builder">Client builder.</param>
    /// <param name="configure">
    ///     Optional configuration delegate to register feature states, reducers, and effects.
    ///     When null, only base Reservoir infrastructure is registered.
    /// </param>
    /// <returns>The same client builder instance for fluent chaining.</returns>
    public static IClientBuilder AddReservoir(
        this IClientBuilder builder,
        Action<IReservoirBuilder>? configure = null
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddReservoir(configure);
        return builder;
    }
}