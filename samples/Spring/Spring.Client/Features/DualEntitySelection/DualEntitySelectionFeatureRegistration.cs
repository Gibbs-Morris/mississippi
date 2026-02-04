using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir;


namespace Spring.Client.Features.DualEntitySelection;

/// <summary>
///     Extension methods for registering the dual entity selection feature.
/// </summary>
internal static class DualEntitySelectionFeatureRegistration
{
    /// <summary>
    ///     Adds the dual entity selection feature to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDualEntitySelectionFeature(
        this IServiceCollection services
    )
    {
        services.AddReducer<SetEntityAIdAction, DualEntitySelectionState>(DualEntitySelectionReducers.SetEntityAId);
        services.AddReducer<SetEntityBIdAction, DualEntitySelectionState>(DualEntitySelectionReducers.SetEntityBId);
        return services;
    }
}