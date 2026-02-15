using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir;

namespace LightSpeed.Client.Features.KitchenSinkFeatures.SectionUi;

/// <summary>
///     Extension methods for registering Kitchen Sink section UI state reducers.
/// </summary>
internal static class KitchenSinkSectionUiFeatureRegistration
{
    /// <summary>
    ///     Adds reducers for Kitchen Sink section UI state.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddKitchenSinkSectionUiFeature(
        this IServiceCollection services
    )
    {
        services.AddReducer<ToggleKitchenSinkSectionEventsAction, KitchenSinkSectionUiState>(KitchenSinkSectionUiReducers.ToggleEventsPanel);
        return services;
    }
}
