using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisLabel;

/// <summary>
///     Extension methods for registering the MisLabel Kitchen Sink feature.
/// </summary>
internal static class MisLabelKitchenSinkFeatureRegistration
{
    /// <summary>
    ///     Adds reducers for the MisLabel Kitchen Sink feature.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMisLabelKitchenSinkFeature(
        this IServiceCollection services
    )
    {
        services.AddReducer<SetMisLabelTextAction, MisLabelKitchenSinkState>(MisLabelKitchenSinkReducers.SetText);
        services.AddReducer<SetMisLabelForAction, MisLabelKitchenSinkState>(MisLabelKitchenSinkReducers.SetFor);
        services.AddReducer<SetMisLabelIsRequiredAction, MisLabelKitchenSinkState>(MisLabelKitchenSinkReducers.SetIsRequired);
        services.AddReducer<SetMisLabelStateAction, MisLabelKitchenSinkState>(MisLabelKitchenSinkReducers.SetState);
        services.AddReducer<SetMisLabelCssClassAction, MisLabelKitchenSinkState>(MisLabelKitchenSinkReducers.SetCssClass);
        return services;
    }
}
