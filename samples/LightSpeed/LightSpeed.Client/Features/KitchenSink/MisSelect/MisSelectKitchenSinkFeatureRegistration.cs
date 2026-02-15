using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisSelect;

/// <summary>
///     Extension methods for registering the MisSelect Kitchen Sink feature.
/// </summary>
internal static class MisSelectKitchenSinkFeatureRegistration
{
    /// <summary>
    ///     Adds reducers for the MisSelect Kitchen Sink feature.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMisSelectKitchenSinkFeature(
        this IServiceCollection services
    )
    {
        services.AddReducer<SetMisSelectValueAction, MisSelectKitchenSinkState>(MisSelectKitchenSinkReducers.SetValue);
        services.AddReducer<SetMisSelectIntentIdAction, MisSelectKitchenSinkState>(
            MisSelectKitchenSinkReducers.SetIntentId);
        services.AddReducer<SetMisSelectAriaLabelAction, MisSelectKitchenSinkState>(
            MisSelectKitchenSinkReducers.SetAriaLabel);
        services.AddReducer<SetMisSelectTitleAction, MisSelectKitchenSinkState>(MisSelectKitchenSinkReducers.SetTitle);
        services.AddReducer<SetMisSelectCssClassAction, MisSelectKitchenSinkState>(
            MisSelectKitchenSinkReducers.SetCssClass);
        services.AddReducer<SetMisSelectPlaceholderAction, MisSelectKitchenSinkState>(
            MisSelectKitchenSinkReducers.SetPlaceholder);
        services.AddReducer<SetMisSelectDisabledAction, MisSelectKitchenSinkState>(
            MisSelectKitchenSinkReducers.SetDisabled);
        services.AddReducer<SetMisSelectRequiredAction, MisSelectKitchenSinkState>(
            MisSelectKitchenSinkReducers.SetRequired);
        services.AddReducer<SetMisSelectStateAction, MisSelectKitchenSinkState>(MisSelectKitchenSinkReducers.SetState);
        services.AddReducer<SetMisSelectOptionsAction, MisSelectKitchenSinkState>(
            MisSelectKitchenSinkReducers.SetOptions);
        services.AddReducer<RecordMisSelectEventAction, MisSelectKitchenSinkState>(
            MisSelectKitchenSinkReducers.RecordEvent);
        services.AddReducer<ClearMisSelectEventsAction, MisSelectKitchenSinkState>(
            MisSelectKitchenSinkReducers.ClearEvents);
        return services;
    }
}