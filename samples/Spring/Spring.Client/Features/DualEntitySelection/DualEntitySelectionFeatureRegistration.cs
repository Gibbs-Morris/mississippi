using Mississippi.Reservoir.Core;


namespace Spring.Client.Features.DualEntitySelection;

/// <summary>
///     Extension methods for registering the dual entity selection feature.
/// </summary>
internal static class DualEntitySelectionFeatureRegistration
{
    /// <summary>
    ///     Adds the dual entity selection feature to the Reservoir store.
    /// </summary>
    /// <param name="builder">The Reservoir builder.</param>
    /// <returns>The same Reservoir builder for fluent chaining.</returns>
    public static IReservoirBuilder AddDualEntitySelectionFeature(
        this IReservoirBuilder builder
    )
    {
        builder.AddFeature<DualEntitySelectionState>(feature =>
        {
            feature.AddReducer<SetEntityAIdAction, DualEntitySelectionState>(DualEntitySelectionReducers.SetEntityAId);
            feature.AddReducer<SetEntityBIdAction, DualEntitySelectionState>(DualEntitySelectionReducers.SetEntityBId);
        });
        return builder;
    }
}