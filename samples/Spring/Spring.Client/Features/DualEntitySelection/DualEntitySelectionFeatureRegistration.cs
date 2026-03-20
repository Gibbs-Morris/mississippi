using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Core;


namespace MississippiSamples.Spring.Client.Features.DualEntitySelection;

/// <summary>
///     Extension methods for registering the dual entity selection feature.
/// </summary>
internal static class DualEntitySelectionFeatureRegistration
{
    /// <summary>
    ///     Adds the dual entity selection feature to the service collection.
    /// </summary>
    /// <param name="reservoir">The Reservoir builder.</param>
    /// <returns>The Reservoir builder for chaining.</returns>
    public static IReservoirBuilder AddDualEntitySelectionFeature(
        this IReservoirBuilder reservoir
    )
    {
        reservoir.AddFeature<DualEntitySelectionState>(feature => feature
            .AddReducer<DualEntitySelectionState, SetEntityAIdAction>(DualEntitySelectionReducers.SetEntityAId)
            .AddReducer<DualEntitySelectionState, SetEntityBIdAction>(DualEntitySelectionReducers.SetEntityBId));
        return reservoir;
    }
}