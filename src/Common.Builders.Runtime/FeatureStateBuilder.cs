using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Builders.Runtime.Abstractions;


namespace Mississippi.Common.Builders.Runtime;

/// <summary>
///     Runtime feature-state sub-builder implementation.
/// </summary>
/// <typeparam name="TFeatureState">Feature-state type.</typeparam>
public sealed class FeatureStateBuilder<TFeatureState> : IFeatureStateBuilder<TFeatureState>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="FeatureStateBuilder{TFeatureState}" /> class.
    /// </summary>
    /// <param name="services">Service collection.</param>
    public FeatureStateBuilder(
        IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
    }

    /// <inheritdoc />
    public TFeatureState? FeatureStateMarker => default;
}