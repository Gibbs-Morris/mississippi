using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisButton;

/// <summary>
///     Extension methods for registering the MisButton Kitchen Sink feature.
/// </summary>
internal static class MisButtonKitchenSinkFeatureRegistration
{
    /// <summary>
    ///     Adds reducers for the MisButton Kitchen Sink feature.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMisButtonKitchenSinkFeature(
        this IServiceCollection services
    )
    {
        services.AddReducer<SetMisButtonIntentIdAction, MisButtonKitchenSinkState>(
            MisButtonKitchenSinkReducers.SetIntentId);
        services.AddReducer<SetMisButtonAriaLabelAction, MisButtonKitchenSinkState>(
            MisButtonKitchenSinkReducers.SetAriaLabel);
        services.AddReducer<SetMisButtonTitleAction, MisButtonKitchenSinkState>(MisButtonKitchenSinkReducers.SetTitle);
        services.AddReducer<SetMisButtonCssClassAction, MisButtonKitchenSinkState>(
            MisButtonKitchenSinkReducers.SetCssClass);
        services.AddReducer<SetMisButtonDisabledAction, MisButtonKitchenSinkState>(
            MisButtonKitchenSinkReducers.SetIsDisabled);
        services.AddReducer<SetMisButtonTypeAction, MisButtonKitchenSinkState>(MisButtonKitchenSinkReducers.SetType);
        services.AddReducer<RecordMisButtonEventAction, MisButtonKitchenSinkState>(
            MisButtonKitchenSinkReducers.RecordEvent);
        services.AddReducer<ClearMisButtonEventsAction, MisButtonKitchenSinkState>(
            MisButtonKitchenSinkReducers.ClearEvents);
        return services;
    }
}