using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisFormField;

/// <summary>
///     Extension methods for registering the MisFormField Kitchen Sink feature.
/// </summary>
internal static class MisFormFieldKitchenSinkFeatureRegistration
{
    /// <summary>
    ///     Adds reducers for the MisFormField Kitchen Sink feature.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMisFormFieldKitchenSinkFeature(
        this IServiceCollection services
    )
    {
        services.AddReducer<SetMisFormFieldLabelTextAction, MisFormFieldKitchenSinkState>(
            MisFormFieldKitchenSinkReducers.SetLabelText);
        services.AddReducer<SetMisFormFieldHelpTextAction, MisFormFieldKitchenSinkState>(
            MisFormFieldKitchenSinkReducers.SetHelpText);
        services.AddReducer<SetMisFormFieldValidationMessageAction, MisFormFieldKitchenSinkState>(
            MisFormFieldKitchenSinkReducers.SetValidationMessage);
        services.AddReducer<SetMisFormFieldInputValueAction, MisFormFieldKitchenSinkState>(
            MisFormFieldKitchenSinkReducers.SetInputValue);
        services.AddReducer<SetMisFormFieldShowValidationAction, MisFormFieldKitchenSinkState>(
            MisFormFieldKitchenSinkReducers.SetShowValidation);
        services.AddReducer<SetMisFormFieldStateAction, MisFormFieldKitchenSinkState>(
            MisFormFieldKitchenSinkReducers.SetState);
        services.AddReducer<SetMisFormFieldDisabledAction, MisFormFieldKitchenSinkState>(
            MisFormFieldKitchenSinkReducers.SetDisabled);
        services.AddReducer<SetMisFormFieldCssClassAction, MisFormFieldKitchenSinkState>(
            MisFormFieldKitchenSinkReducers.SetCssClass);
        return services;
    }
}