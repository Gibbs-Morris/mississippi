using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisSearchInput;

/// <summary>
///     Extension methods for registering the MisSearchInput Kitchen Sink feature.
/// </summary>
internal static class MisSearchInputKitchenSinkFeatureRegistration
{
    /// <summary>
    ///     Adds reducers for the MisSearchInput Kitchen Sink feature.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMisSearchInputKitchenSinkFeature(
        this IServiceCollection services
    )
    {
        services.AddReducer<SetMisSearchInputValueAction, MisSearchInputKitchenSinkState>(
            MisSearchInputKitchenSinkReducers.SetValue);
        services.AddReducer<SetMisSearchInputDisabledAction, MisSearchInputKitchenSinkState>(
            MisSearchInputKitchenSinkReducers.SetDisabled);
        services.AddReducer<SetMisSearchInputPlaceholderAction, MisSearchInputKitchenSinkState>(
            MisSearchInputKitchenSinkReducers.SetPlaceholder);
        services.AddReducer<SetMisSearchInputAriaLabelAction, MisSearchInputKitchenSinkState>(
            MisSearchInputKitchenSinkReducers.SetAriaLabel);
        services.AddReducer<SetMisSearchInputCssClassAction, MisSearchInputKitchenSinkState>(
            MisSearchInputKitchenSinkReducers.SetCssClass);
        services.AddReducer<RecordMisSearchInputEventAction, MisSearchInputKitchenSinkState>(
            MisSearchInputKitchenSinkReducers.RecordEvent);
        services.AddReducer<ClearMisSearchInputEventsAction, MisSearchInputKitchenSinkState>(
            MisSearchInputKitchenSinkReducers.ClearEvents);
        return services;
    }
}