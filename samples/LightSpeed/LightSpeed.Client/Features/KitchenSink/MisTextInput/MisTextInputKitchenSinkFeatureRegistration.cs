using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisTextInput;

/// <summary>
///     Extension methods for registering the MisTextInput Kitchen Sink feature.
/// </summary>
internal static class MisTextInputKitchenSinkFeatureRegistration
{
    /// <summary>
    ///     Adds reducers for the MisTextInput Kitchen Sink feature.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMisTextInputKitchenSinkFeature(
        this IServiceCollection services
    )
    {
        services.AddReducer<SetMisTextInputValueAction, MisTextInputKitchenSinkState>(MisTextInputKitchenSinkReducers.SetValue);
        services.AddReducer<SetMisTextInputIntentIdAction, MisTextInputKitchenSinkState>(MisTextInputKitchenSinkReducers.SetIntentId);
        services.AddReducer<SetMisTextInputAriaLabelAction, MisTextInputKitchenSinkState>(MisTextInputKitchenSinkReducers.SetAriaLabel);
        services.AddReducer<SetMisTextInputPlaceholderAction, MisTextInputKitchenSinkState>(MisTextInputKitchenSinkReducers.SetPlaceholder);
        services.AddReducer<SetMisTextInputTitleAction, MisTextInputKitchenSinkState>(MisTextInputKitchenSinkReducers.SetTitle);
        services.AddReducer<SetMisTextInputCssClassAction, MisTextInputKitchenSinkState>(MisTextInputKitchenSinkReducers.SetCssClass);
        services.AddReducer<SetMisTextInputAutoCompleteAction, MisTextInputKitchenSinkState>(MisTextInputKitchenSinkReducers.SetAutoComplete);
        services.AddReducer<SetMisTextInputDisabledAction, MisTextInputKitchenSinkState>(MisTextInputKitchenSinkReducers.SetDisabled);
        services.AddReducer<SetMisTextInputReadOnlyAction, MisTextInputKitchenSinkState>(MisTextInputKitchenSinkReducers.SetReadOnly);
        services.AddReducer<SetMisTextInputTypeAction, MisTextInputKitchenSinkState>(MisTextInputKitchenSinkReducers.SetType);
        services.AddReducer<RecordMisTextInputEventAction, MisTextInputKitchenSinkState>(MisTextInputKitchenSinkReducers.RecordEvent);
        services.AddReducer<ClearMisTextInputEventsAction, MisTextInputKitchenSinkState>(MisTextInputKitchenSinkReducers.ClearEvents);
        return services;
    }
}
