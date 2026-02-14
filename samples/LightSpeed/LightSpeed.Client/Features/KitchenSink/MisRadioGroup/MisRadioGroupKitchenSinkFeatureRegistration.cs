using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisRadioGroup;

/// <summary>
///     Extension methods for registering the MisRadioGroup Kitchen Sink feature.
/// </summary>
internal static class MisRadioGroupKitchenSinkFeatureRegistration
{
    /// <summary>
    ///     Adds reducers for the MisRadioGroup Kitchen Sink feature.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMisRadioGroupKitchenSinkFeature(
        this IServiceCollection services
    )
    {
        services.AddReducer<SetMisRadioGroupValueAction, MisRadioGroupKitchenSinkState>(MisRadioGroupKitchenSinkReducers.SetValue);
        services.AddReducer<SetMisRadioGroupIntentIdAction, MisRadioGroupKitchenSinkState>(MisRadioGroupKitchenSinkReducers.SetIntentId);
        services.AddReducer<SetMisRadioGroupAriaLabelAction, MisRadioGroupKitchenSinkState>(MisRadioGroupKitchenSinkReducers.SetAriaLabel);
        services.AddReducer<SetMisRadioGroupTitleAction, MisRadioGroupKitchenSinkState>(MisRadioGroupKitchenSinkReducers.SetTitle);
        services.AddReducer<SetMisRadioGroupCssClassAction, MisRadioGroupKitchenSinkState>(MisRadioGroupKitchenSinkReducers.SetCssClass);
        services.AddReducer<SetMisRadioGroupDisabledAction, MisRadioGroupKitchenSinkState>(MisRadioGroupKitchenSinkReducers.SetDisabled);
        services.AddReducer<SetMisRadioGroupRequiredAction, MisRadioGroupKitchenSinkState>(MisRadioGroupKitchenSinkReducers.SetRequired);
        services.AddReducer<SetMisRadioGroupStateAction, MisRadioGroupKitchenSinkState>(MisRadioGroupKitchenSinkReducers.SetState);
        services.AddReducer<SetMisRadioGroupOptionsAction, MisRadioGroupKitchenSinkState>(MisRadioGroupKitchenSinkReducers.SetOptions);
        services.AddReducer<RecordMisRadioGroupEventAction, MisRadioGroupKitchenSinkState>(MisRadioGroupKitchenSinkReducers.RecordEvent);
        services.AddReducer<ClearMisRadioGroupEventsAction, MisRadioGroupKitchenSinkState>(MisRadioGroupKitchenSinkReducers.ClearEvents);
        return services;
    }
}
