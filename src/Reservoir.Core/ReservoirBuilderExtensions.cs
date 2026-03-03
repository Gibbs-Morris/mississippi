using System;
using System.Collections.Generic;

using Mississippi.Common.Builders.Abstractions;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir.Core;

/// <summary>
///     Extension methods for configuring features on an <see cref="IReservoirBuilder" />.
/// </summary>
public static class ReservoirBuilderExtensions
{
    /// <summary>
    ///     Adds a feature state to the Reservoir store with builder-based configuration.
    /// </summary>
    /// <typeparam name="TState">The feature state type.</typeparam>
    /// <param name="builder">The Reservoir builder.</param>
    /// <param name="configure">Configuration delegate for the feature state builder.</param>
    /// <returns>The same Reservoir builder for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="builder" /> or <paramref name="configure" /> is null.
    /// </exception>
    /// <exception cref="BuilderValidationException">
    ///     Thrown when no reducers are configured for the feature state.
    /// </exception>
    /// <remarks>
    ///     <para>
    ///         The feature builder shares the parent Reservoir builder's <see cref="IMississippiBuilder.Services" />
    ///         collection. Reducer and effect registrations are delegated to
    ///         <see cref="ReservoirRegistrations" /> methods.
    ///     </para>
    ///     <para>
    ///         Validation is inline: the feature builder's <see cref="IMississippiBuilder.Validate" /> method
    ///         is called immediately after the configure delegate executes.
    ///     </para>
    /// </remarks>
    public static IReservoirBuilder AddFeature<TState>(
        this IReservoirBuilder builder,
        Action<IFeatureStateBuilder<TState>> configure
    )
        where TState : class, IFeatureState, new()
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);
        FeatureStateBuilder<TState> featureBuilder = new(builder.Services);
        configure(featureBuilder);
        IReadOnlyList<BuilderDiagnostic> diagnostics = featureBuilder.Validate();
        if (diagnostics.Count > 0)
        {
            throw new BuilderValidationException(
                $"Feature state '{typeof(TState).Name}' validation failed.",
                diagnostics);
        }

        ((ReservoirBuilder)builder).IncrementFeatureCount();
        return builder;
    }
}