using Mississippi.Reservoir.Abstractions;


namespace MississippiSamples.Spring.Client.Features.DualEntitySelection;

/// <summary>
///     Extension methods for registering the dual entity selection feature.
/// </summary>
internal static class DualEntitySelectionFeatureRegistration
{
    /// <summary>
    ///     Adds the dual entity selection feature to the service collection.
    /// </summary>
    /// <param name="builder">The Reservoir builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IReservoirBuilder AddDualEntitySelectionFeature(
        this IReservoirBuilder builder
    )
    {
        builder.AddFeatureState<DualEntitySelectionState>(feature => feature
            .AddReducer<SetEntityAIdAction>(DualEntitySelectionReducers.SetEntityAId)
            .AddReducer<SetEntityBIdAction>(DualEntitySelectionReducers.SetEntityBId));
        return builder;
    }
}