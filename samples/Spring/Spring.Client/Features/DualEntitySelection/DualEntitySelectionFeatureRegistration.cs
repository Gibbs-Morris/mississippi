using System;

using Mississippi.Reservoir.Abstractions.Builders;


namespace Spring.Client.Features.DualEntitySelection;

/// <summary>
///     Extension methods for registering the dual entity selection feature.
/// </summary>
internal static class DualEntitySelectionFeatureRegistration
{
    /// <summary>
    ///     Adds the dual entity selection feature to the Reservoir builder.
    /// </summary>
    /// <param name="builder">The Reservoir builder.</param>
    /// <returns>The Reservoir builder for chaining.</returns>
    public static IReservoirBuilder AddDualEntitySelectionFeature(
        this IReservoirBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddFeature<DualEntitySelectionState>(featureBuilder =>
        {
            featureBuilder.AddReducer<SetEntityAIdAction>(DualEntitySelectionReducers.SetEntityAId);
            featureBuilder.AddReducer<SetEntityBIdAction>(DualEntitySelectionReducers.SetEntityBId);
        });
        return builder;
    }
}