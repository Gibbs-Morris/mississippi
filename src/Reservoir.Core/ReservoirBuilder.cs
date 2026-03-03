using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Builders.Abstractions;


namespace Mississippi.Reservoir.Core;

/// <summary>
///     Internal builder for configuring Reservoir state management features.
/// </summary>
internal sealed class ReservoirBuilder : IReservoirBuilder
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ReservoirBuilder" /> class.
    /// </summary>
    /// <param name="services">The parent service collection.</param>
    public ReservoirBuilder(
        IServiceCollection services
    ) =>
        Services = services;

    /// <inheritdoc />
    public IServiceCollection Services { get; }

    private int FeatureCount { get; set; }

    /// <summary>
    ///     Increments the feature count for validation tracking.
    /// </summary>
    public void IncrementFeatureCount() => FeatureCount++;

    /// <inheritdoc />
    public IReadOnlyList<BuilderDiagnostic> Validate() =>
        FeatureCount > 0
            ? []
            :
            [
                new(
                    "Reservoir.NoFeaturesConfigured",
                    "Reservoir",
                    "No feature states configured.",
                    "Call reservoir.AddFeature<TState>(...) to register at least one feature state."),
            ];
}