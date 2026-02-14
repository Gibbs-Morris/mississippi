using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisTextarea;

/// <summary>
///     Extension methods for registering the MisTextarea Kitchen Sink feature.
/// </summary>
internal static class MisTextareaKitchenSinkFeatureRegistration
{
    /// <summary>
    ///     Adds reducers for the MisTextarea Kitchen Sink feature.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMisTextareaKitchenSinkFeature(
        this IServiceCollection services
    )
    {
        services.AddReducer<SetMisTextareaValueAction, MisTextareaKitchenSinkState>(MisTextareaKitchenSinkReducers.SetValue);
        services.AddReducer<SetMisTextareaIntentIdAction, MisTextareaKitchenSinkState>(MisTextareaKitchenSinkReducers.SetIntentId);
        services.AddReducer<SetMisTextareaAriaLabelAction, MisTextareaKitchenSinkState>(MisTextareaKitchenSinkReducers.SetAriaLabel);
        services.AddReducer<SetMisTextareaTitleAction, MisTextareaKitchenSinkState>(MisTextareaKitchenSinkReducers.SetTitle);
        services.AddReducer<SetMisTextareaCssClassAction, MisTextareaKitchenSinkState>(MisTextareaKitchenSinkReducers.SetCssClass);
        services.AddReducer<SetMisTextareaPlaceholderAction, MisTextareaKitchenSinkState>(MisTextareaKitchenSinkReducers.SetPlaceholder);
        services.AddReducer<SetMisTextareaRowsAction, MisTextareaKitchenSinkState>(MisTextareaKitchenSinkReducers.SetRows);
        services.AddReducer<SetMisTextareaDisabledAction, MisTextareaKitchenSinkState>(MisTextareaKitchenSinkReducers.SetDisabled);
        services.AddReducer<SetMisTextareaReadOnlyAction, MisTextareaKitchenSinkState>(MisTextareaKitchenSinkReducers.SetReadOnly);
        services.AddReducer<SetMisTextareaRequiredAction, MisTextareaKitchenSinkState>(MisTextareaKitchenSinkReducers.SetRequired);
        services.AddReducer<SetMisTextareaStateAction, MisTextareaKitchenSinkState>(MisTextareaKitchenSinkReducers.SetState);
        services.AddReducer<RecordMisTextareaEventAction, MisTextareaKitchenSinkState>(MisTextareaKitchenSinkReducers.RecordEvent);
        services.AddReducer<ClearMisTextareaEventsAction, MisTextareaKitchenSinkState>(MisTextareaKitchenSinkReducers.ClearEvents);
        return services;
    }
}