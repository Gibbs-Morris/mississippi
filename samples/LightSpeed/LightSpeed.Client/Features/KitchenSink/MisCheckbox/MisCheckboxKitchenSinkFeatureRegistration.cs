using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisCheckbox;

/// <summary>
///     Extension methods for registering the MisCheckbox Kitchen Sink feature.
/// </summary>
internal static class MisCheckboxKitchenSinkFeatureRegistration
{
    /// <summary>
    ///     Adds reducers for the MisCheckbox Kitchen Sink feature.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMisCheckboxKitchenSinkFeature(
        this IServiceCollection services
    )
    {
        services.AddReducer<SetMisCheckboxCheckedAction, MisCheckboxKitchenSinkState>(
            MisCheckboxKitchenSinkReducers.SetChecked);
        services.AddReducer<SetMisCheckboxIntentIdAction, MisCheckboxKitchenSinkState>(
            MisCheckboxKitchenSinkReducers.SetIntentId);
        services.AddReducer<SetMisCheckboxAriaLabelAction, MisCheckboxKitchenSinkState>(
            MisCheckboxKitchenSinkReducers.SetAriaLabel);
        services.AddReducer<SetMisCheckboxTitleAction, MisCheckboxKitchenSinkState>(
            MisCheckboxKitchenSinkReducers.SetTitle);
        services.AddReducer<SetMisCheckboxCssClassAction, MisCheckboxKitchenSinkState>(
            MisCheckboxKitchenSinkReducers.SetCssClass);
        services.AddReducer<SetMisCheckboxDisabledAction, MisCheckboxKitchenSinkState>(
            MisCheckboxKitchenSinkReducers.SetDisabled);
        services.AddReducer<SetMisCheckboxRequiredAction, MisCheckboxKitchenSinkState>(
            MisCheckboxKitchenSinkReducers.SetRequired);
        services.AddReducer<SetMisCheckboxValueAction, MisCheckboxKitchenSinkState>(
            MisCheckboxKitchenSinkReducers.SetValue);
        services.AddReducer<SetMisCheckboxStateAction, MisCheckboxKitchenSinkState>(
            MisCheckboxKitchenSinkReducers.SetState);
        services.AddReducer<RecordMisCheckboxEventAction, MisCheckboxKitchenSinkState>(
            MisCheckboxKitchenSinkReducers.RecordEvent);
        services.AddReducer<ClearMisCheckboxEventsAction, MisCheckboxKitchenSinkState>(
            MisCheckboxKitchenSinkReducers.ClearEvents);
        return services;
    }
}