using LightSpeed.Client.Features.KitchenSinkFeatures.MisButton;
using LightSpeed.Client.Features.KitchenSinkFeatures.MisCheckbox;
using LightSpeed.Client.Features.KitchenSinkFeatures.MisRadioGroup;
using LightSpeed.Client.Features.KitchenSinkFeatures.MisSelect;
using LightSpeed.Client.Features.KitchenSinkFeatures.MisTextInput;

using Microsoft.Extensions.DependencyInjection;


namespace LightSpeed.Client.Features.KitchenSinkFeatures;

/// <summary>
///     Extension methods for registering Kitchen Sink features.
/// </summary>
public static class KitchenSinkFeatureRegistration
{
    /// <summary>
    ///     Adds all Kitchen Sink feature slices to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddKitchenSinkFeature(
        this IServiceCollection services
    )
    {
        services.AddMisButtonKitchenSinkFeature();
        services.AddMisCheckboxKitchenSinkFeature();
        services.AddMisRadioGroupKitchenSinkFeature();
        services.AddMisTextInputKitchenSinkFeature();
        services.AddMisSelectKitchenSinkFeature();
        return services;
    }
}