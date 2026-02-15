using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisSwitch;

/// <summary>
///     Extension methods for registering the MisSwitch Kitchen Sink feature.
/// </summary>
internal static class MisSwitchKitchenSinkFeatureRegistration
{
    /// <summary>
    ///     Adds reducers for the MisSwitch Kitchen Sink feature.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMisSwitchKitchenSinkFeature(
        this IServiceCollection services
    )
    {
        services.AddReducer<SetMisSwitchCheckedAction, MisSwitchKitchenSinkState>(
            MisSwitchKitchenSinkReducers.SetChecked);
        services.AddReducer<SetMisSwitchValueAction, MisSwitchKitchenSinkState>(MisSwitchKitchenSinkReducers.SetValue);
        services.AddReducer<SetMisSwitchIntentIdAction, MisSwitchKitchenSinkState>(
            MisSwitchKitchenSinkReducers.SetIntentId);
        services.AddReducer<SetMisSwitchAriaLabelAction, MisSwitchKitchenSinkState>(
            MisSwitchKitchenSinkReducers.SetAriaLabel);
        services.AddReducer<SetMisSwitchTitleAction, MisSwitchKitchenSinkState>(MisSwitchKitchenSinkReducers.SetTitle);
        services.AddReducer<SetMisSwitchCssClassAction, MisSwitchKitchenSinkState>(
            MisSwitchKitchenSinkReducers.SetCssClass);
        services.AddReducer<SetMisSwitchDisabledAction, MisSwitchKitchenSinkState>(
            MisSwitchKitchenSinkReducers.SetDisabled);
        services.AddReducer<SetMisSwitchRequiredAction, MisSwitchKitchenSinkState>(
            MisSwitchKitchenSinkReducers.SetRequired);
        services.AddReducer<SetMisSwitchStateAction, MisSwitchKitchenSinkState>(MisSwitchKitchenSinkReducers.SetState);
        services.AddReducer<RecordMisSwitchEventAction, MisSwitchKitchenSinkState>(
            MisSwitchKitchenSinkReducers.RecordEvent);
        services.AddReducer<ClearMisSwitchEventsAction, MisSwitchKitchenSinkState>(
            MisSwitchKitchenSinkReducers.ClearEvents);
        return services;
    }
}