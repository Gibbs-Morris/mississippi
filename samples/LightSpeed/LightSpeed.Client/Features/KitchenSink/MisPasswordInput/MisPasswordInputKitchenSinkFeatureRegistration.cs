using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisPasswordInput;

/// <summary>
///     Extension methods for registering the MisPasswordInput Kitchen Sink feature.
/// </summary>
internal static class MisPasswordInputKitchenSinkFeatureRegistration
{
    /// <summary>
    ///     Adds reducers for the MisPasswordInput Kitchen Sink feature.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMisPasswordInputKitchenSinkFeature(
        this IServiceCollection services
    )
    {
        services.AddReducer<SetMisPasswordInputValueAction, MisPasswordInputKitchenSinkState>(MisPasswordInputKitchenSinkReducers.SetValue);
        services.AddReducer<SetMisPasswordInputVisibilityAction, MisPasswordInputKitchenSinkState>(MisPasswordInputKitchenSinkReducers.SetIsPasswordVisible);
        services.AddReducer<SetMisPasswordInputDisabledAction, MisPasswordInputKitchenSinkState>(MisPasswordInputKitchenSinkReducers.SetDisabled);
        services.AddReducer<SetMisPasswordInputPlaceholderAction, MisPasswordInputKitchenSinkState>(MisPasswordInputKitchenSinkReducers.SetPlaceholder);
        services.AddReducer<SetMisPasswordInputAriaLabelAction, MisPasswordInputKitchenSinkState>(MisPasswordInputKitchenSinkReducers.SetAriaLabel);
        services.AddReducer<SetMisPasswordInputCssClassAction, MisPasswordInputKitchenSinkState>(MisPasswordInputKitchenSinkReducers.SetCssClass);
        services.AddReducer<SetMisPasswordInputReadOnlyAction, MisPasswordInputKitchenSinkState>(MisPasswordInputKitchenSinkReducers.SetReadOnly);
        services.AddReducer<SetMisPasswordInputStateAction, MisPasswordInputKitchenSinkState>(MisPasswordInputKitchenSinkReducers.SetState);
        services.AddReducer<RecordMisPasswordInputEventAction, MisPasswordInputKitchenSinkState>(MisPasswordInputKitchenSinkReducers.RecordEvent);
        services.AddReducer<ClearMisPasswordInputEventsAction, MisPasswordInputKitchenSinkState>(MisPasswordInputKitchenSinkReducers.ClearEvents);
        return services;
    }
}
