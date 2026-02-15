using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisCheckboxGroup;

/// <summary>
///     Extension methods for registering the MisCheckboxGroup Kitchen Sink feature.
/// </summary>
internal static class MisCheckboxGroupKitchenSinkFeatureRegistration
{
    /// <summary>
    ///     Adds reducers for the MisCheckboxGroup Kitchen Sink feature.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMisCheckboxGroupKitchenSinkFeature(
        this IServiceCollection services
    )
    {
        services.AddReducer<SetMisCheckboxGroupValuesAction, MisCheckboxGroupKitchenSinkState>(
            MisCheckboxGroupKitchenSinkReducers.SetValues);
        services.AddReducer<SetMisCheckboxGroupDisabledAction, MisCheckboxGroupKitchenSinkState>(
            MisCheckboxGroupKitchenSinkReducers.SetDisabled);
        services.AddReducer<SetMisCheckboxGroupRequiredAction, MisCheckboxGroupKitchenSinkState>(
            MisCheckboxGroupKitchenSinkReducers.SetRequired);
        services.AddReducer<SetMisCheckboxGroupAriaLabelAction, MisCheckboxGroupKitchenSinkState>(
            MisCheckboxGroupKitchenSinkReducers.SetAriaLabel);
        services.AddReducer<SetMisCheckboxGroupCssClassAction, MisCheckboxGroupKitchenSinkState>(
            MisCheckboxGroupKitchenSinkReducers.SetCssClass);
        services.AddReducer<SetMisCheckboxGroupOptionsAction, MisCheckboxGroupKitchenSinkState>(
            MisCheckboxGroupKitchenSinkReducers.SetOptions);
        services.AddReducer<RecordMisCheckboxGroupEventAction, MisCheckboxGroupKitchenSinkState>(
            MisCheckboxGroupKitchenSinkReducers.RecordEvent);
        services.AddReducer<ClearMisCheckboxGroupEventsAction, MisCheckboxGroupKitchenSinkState>(
            MisCheckboxGroupKitchenSinkReducers.ClearEvents);
        return services;
    }
}