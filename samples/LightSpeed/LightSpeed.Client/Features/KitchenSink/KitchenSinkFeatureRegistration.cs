using LightSpeed.Client.Features.KitchenSinkFeatures.MisButton;
using LightSpeed.Client.Features.KitchenSinkFeatures.MisCheckbox;
using LightSpeed.Client.Features.KitchenSinkFeatures.MisCheckboxGroup;
using LightSpeed.Client.Features.KitchenSinkFeatures.MisFormField;
using LightSpeed.Client.Features.KitchenSinkFeatures.MisHelpText;
using LightSpeed.Client.Features.KitchenSinkFeatures.MisLabel;
using LightSpeed.Client.Features.KitchenSinkFeatures.MisPasswordInput;
using LightSpeed.Client.Features.KitchenSinkFeatures.MisRadioGroup;
using LightSpeed.Client.Features.KitchenSinkFeatures.MisSearchInput;
using LightSpeed.Client.Features.KitchenSinkFeatures.MisSelect;
using LightSpeed.Client.Features.KitchenSinkFeatures.MisSwitch;
using LightSpeed.Client.Features.KitchenSinkFeatures.MisTextarea;
using LightSpeed.Client.Features.KitchenSinkFeatures.MisTextInput;
using LightSpeed.Client.Features.KitchenSinkFeatures.MisValidationMessage;
using LightSpeed.Client.Features.KitchenSinkFeatures.SectionUi;

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
        services.AddMisCheckboxGroupKitchenSinkFeature();
        services.AddMisFormFieldKitchenSinkFeature();
        services.AddMisHelpTextKitchenSinkFeature();
        services.AddMisLabelKitchenSinkFeature();
        services.AddMisPasswordInputKitchenSinkFeature();
        services.AddMisRadioGroupKitchenSinkFeature();
        services.AddMisSearchInputKitchenSinkFeature();
        services.AddMisSelectKitchenSinkFeature();
        services.AddMisSwitchKitchenSinkFeature();
        services.AddMisTextareaKitchenSinkFeature();
        services.AddMisTextInputKitchenSinkFeature();
        services.AddMisValidationMessageKitchenSinkFeature();
        services.AddKitchenSinkSectionUiFeature();
        return services;
    }
}